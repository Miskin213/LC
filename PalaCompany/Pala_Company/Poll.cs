using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace PalaCompany
{
    internal enum PollHudPhase
    {
        Hidden,
        Open,
        Result
    }

    internal sealed class PollHudState
    {
        internal PollHudPhase Phase;
        internal string Option1;
        internal string Option2;
        internal int Votes1;
        internal int Votes2;
        internal float SecondsLeft;
        internal string ResultText;
        internal string LatestVoteUsername;
        internal string LatestVoteOption;

        internal static PollHudState Hidden => new PollHudState { Phase = PollHudPhase.Hidden };
    }

    internal sealed class PollSpawnResult
    {
        internal bool Spawned;
        internal string FailureReason;
        internal string TargetPlayerName;
    }

    internal sealed class Poll
    {
        private readonly Action<string> _log;
        private readonly Func<IList<EnemyType>> _getCandidates;
        private readonly Func<EnemyType, PollSpawnResult> _trySpawn;
        private readonly Action _playSound;
        private readonly string _filePath;
        private readonly System.Random _random = new System.Random();
        private readonly Dictionary<string, int> _votes =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private EnemyType _option1;
        private EnemyType _option2;
        private float _voteRemaining;
        private float _cycleRemaining;
        private float _resultRemaining;
        private float _hudResultRemaining;
        private float _lastUpdateTime;
        private float _previewRemaining;
        private float _lastFileUpdateTime = float.NegativeInfinity;
        private bool _hasUpdatedTime;
        private bool _cycleInitialized;
        private bool _debugForced;
        private bool _loggedInsufficientCandidates;
        private bool _loggedPollLimitReached;
        private bool _previousShipLanded;
        private bool _loggedFileError;
        private int _pollsOpenedThisRound;
        private string _resultText = "";
        private string _hudResultText = "";
        private string _resultTargetPlayer;
        private string _latestVoteUsername;
        private int _latestVoteOption;
        private string _lastFileText;

        internal Poll(
            Action<string> log,
            Func<IList<EnemyType>> getCandidates,
            Func<EnemyType, PollSpawnResult> trySpawn,
            Action playSound,
            string filePath)
        {
            _log = log;
            _getCandidates = getCandidates;
            _trySpawn = trySpawn;
            _playSound = playSound;
            _filePath = filePath;
        }

        internal bool IsOpen => _option1 != null && _option2 != null;

        internal void ShowDebugPreview(float seconds)
        {
            _previewRemaining = Math.Max(0f, seconds);
        }

        internal PollHudState GetHudState()
        {
            if (_previewRemaining > 0f)
            {
                return new PollHudState
                {
                    Phase = PollHudPhase.Open,
                    Option1 = Loc.T("poll.debug.option1"),
                    Option2 = Loc.T("poll.debug.option2"),
                    Votes1 = 5,
                    Votes2 = 3,
                    SecondsLeft = _previewRemaining,
                    LatestVoteUsername = null,
                    LatestVoteOption = null
                };
            }

            if (IsOpen)
            {
                return new PollHudState
                {
                    Phase = PollHudPhase.Open,
                    Option1 = _option1.enemyName,
                    Option2 = _option2.enemyName,
                    Votes1 = _votes.Count(vote => vote.Value == 1),
                    Votes2 = _votes.Count(vote => vote.Value == 2),
                    SecondsLeft = _voteRemaining,
                    LatestVoteUsername = _latestVoteUsername,
                    LatestVoteOption = _latestVoteOption == 1 ? _option1.enemyName
                        : _latestVoteOption == 2 ? _option2.enemyName : null
                };
            }

            if (_hudResultRemaining > 0f)
            {
                return new PollHudState
                {
                    Phase = PollHudPhase.Result,
                    ResultText = _hudResultText
                };
            }

            return PollHudState.Hidden;
        }

        internal static bool TryParseVote(string text, out int option)
        {
            string command = (text ?? "").Trim();
            if (string.Equals(command, "!1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(command, "!vote 1", StringComparison.OrdinalIgnoreCase))
            {
                option = 1;
                return true;
            }
            if (string.Equals(command, "!2", StringComparison.OrdinalIgnoreCase)
                || string.Equals(command, "!vote 2", StringComparison.OrdinalIgnoreCase))
            {
                option = 2;
                return true;
            }
            option = 0;
            return false;
        }

        internal bool CastVote(string username, int option, bool allowVoteChange)
        {
            if (!IsOpen || (option != 1 && option != 2) || string.IsNullOrWhiteSpace(username))
                return false;
            if (_votes.ContainsKey(username) && !allowVoteChange)
                return false;
            _votes[username] = option;
            _latestVoteUsername = username;
            _latestVoteOption = option;
            return true;
        }

        internal void Update(
            bool enabled,
            bool hostInRound,
            bool shipHasLanded,
            float cycleSeconds,
            float voteSeconds,
            int maxPollsPerRound,
            string noVotesBehavior,
            bool showTargetPlayer,
            bool writePollFile,
            bool forceToggle)
        {
            float now = Time.time;
            float elapsed = _hasUpdatedTime ? Math.Max(0f, now - _lastUpdateTime) : 0f;
            _lastUpdateTime = now;
            _hasUpdatedTime = true;
            if (shipHasLanded && !_previousShipLanded)
            {
                _pollsOpenedThisRound = 0;
                _loggedPollLimitReached = false;
            }
            _previousShipLanded = shipHasLanded;

            if (!hostInRound)
            {
                _previewRemaining = 0f;
                if (IsOpen)
                {
                    _log("Poll cancelled because the host is no longer in a landed round.");
                    ClearOpenPoll();
                    _debugForced = false;
                }
                UpdatePollFile(now, writePollFile);
                return;
            }

            if (_previewRemaining > 0f)
                _previewRemaining = Math.Max(0f, _previewRemaining - elapsed);

            if (forceToggle)
            {
                if (IsOpen)
                    ClosePoll(cycleSeconds, noVotesBehavior, showTargetPlayer);
                else
                {
                    _debugForced = true;
                    if (!OpenPoll(voteSeconds, maxPollsPerRound))
                    {
                        _debugForced = false;
                        _cycleRemaining = 5f;
                        _cycleInitialized = true;
                    }
                }
                UpdatePollFile(now, writePollFile);
                return;
            }

            if (_resultRemaining > 0f)
            {
                _resultRemaining = Math.Max(0f, _resultRemaining - elapsed);
                if (_resultRemaining == 0f)
                    _resultText = "";
            }
            if (_hudResultRemaining > 0f)
                _hudResultRemaining = Math.Max(0f, _hudResultRemaining - elapsed);

            if (IsOpen && !enabled && !_debugForced)
            {
                _log("Poll cancelled because polls are disabled.");
                ClearOpenPoll();
            }

            if (IsOpen)
            {
                _voteRemaining -= elapsed;
                if (_voteRemaining <= 0f)
                    ClosePoll(cycleSeconds, noVotesBehavior, showTargetPlayer);
            }
            else if (enabled)
            {
                if (!_cycleInitialized)
                {
                    _cycleRemaining = Math.Max(1f, cycleSeconds);
                    _cycleInitialized = true;
                }
                else
                {
                    _cycleRemaining -= elapsed;
                    if (_cycleRemaining <= 0f)
                    {
                        if (!OpenPoll(voteSeconds, maxPollsPerRound))
                            _cycleRemaining = 5f;
                    }
                }
            }
            else
            {
                _cycleInitialized = false;
            }

            UpdatePollFile(now, writePollFile);
        }

        private bool OpenPoll(float voteSeconds, int maxPollsPerRound)
        {
            if (maxPollsPerRound > 0 && _pollsOpenedThisRound >= maxPollsPerRound)
            {
                LogPollLimitReached();
                return false;
            }

            var candidates = _getCandidates().Where(type => type != null).Distinct().ToList();
            if (candidates.Count < 2)
            {
                if (!_loggedInsufficientCandidates)
                {
                    _log("Poll skipped: fewer than two spawnable enemy types are available; retrying shortly.");
                    _loggedInsufficientCandidates = true;
                }
                return false;
            }

            _loggedInsufficientCandidates = false;
            int first = _random.Next(candidates.Count);
            _option1 = candidates[first];
            candidates.RemoveAt(first);
            _option2 = candidates[_random.Next(candidates.Count)];
            _votes.Clear();
            _latestVoteUsername = null;
            _latestVoteOption = 0;
            _pollsOpenedThisRound++;
            if (maxPollsPerRound > 0 && _pollsOpenedThisRound >= maxPollsPerRound)
                LogPollLimitReached();
            _voteRemaining = Math.Max(1f, voteSeconds);
            _resultRemaining = 0f;
            _hudResultRemaining = 0f;
            _resultText = "";
            _hudResultText = "";
            _playSound();
            return true;
        }

        private void LogPollLimitReached()
        {
            if (_loggedPollLimitReached) return;
            _log("Poll limit reached for this round.");
            _loggedPollLimitReached = true;
        }

        private void ClosePoll(float cycleSeconds, string noVotesBehavior, bool showTargetPlayer)
        {
            if (!IsOpen) return;

            int votes1 = _votes.Count(vote => vote.Value == 1);
            int votes2 = _votes.Count(vote => vote.Value == 2);
            _playSound();
            string option1Name = _option1.enemyName;
            string option2Name = _option2.enemyName;
            EnemyType winner = null;
            if (votes1 == 0 && votes2 == 0)
            {
                if (string.Equals((noVotesBehavior ?? "").Trim(), "Random", StringComparison.OrdinalIgnoreCase))
                    winner = _random.Next(2) == 0 ? _option1 : _option2;
            }
            else if (votes1 == votes2)
            {
                winner = _random.Next(2) == 0 ? _option1 : _option2;
            }
            else
            {
                winner = votes1 > votes2 ? _option1 : _option2;
            }

            string voteSummary = Loc.T(
                "poll.vote.summary",
                option1Name,
                votes1,
                option2Name,
                votes2);
            if (winner == null)
            {
                _resultText = Loc.T("poll.result.file.skipped");
                _hudResultText = Loc.T(
                    "poll.hud.result",
                    Loc.T("poll.hud.result.header"),
                    Loc.T("poll.hud.result.skipped.body"),
                    "");
                _log("Poll closed with no votes; skipped.");
            }
            else
            {
                _resultText = Loc.T("poll.result.file.winner", winner.enemyName, voteSummary);
            }

            _resultTargetPlayer = null;
            ClearOpenPoll();
            _resultRemaining = 10f;
            _hudResultRemaining = 8f;
            _cycleRemaining = Math.Max(1f, cycleSeconds);
            _cycleInitialized = true;
            _debugForced = false;

            if (winner == null)
                return;

            var spawnResult = _trySpawn(winner);
            string failureReason = spawnResult != null && spawnResult.Spawned
                ? null
                : spawnResult != null ? spawnResult.FailureReason : null;
            if (spawnResult == null || !spawnResult.Spawned)
            {
                if (failureReason == string.Empty || failureReason == null)
                    failureReason = "spawn failed; see the plugin log for details";
            }
            if (!string.IsNullOrEmpty(failureReason))
                _log("Poll spawn failed: " + failureReason);

            _resultTargetPlayer = spawnResult != null && spawnResult.Spawned
                ? spawnResult.TargetPlayerName
                : null;
            string resultLine = Loc.T(
                "poll.hud.result.won",
                winner.enemyName,
                votes1 > votes2 ? votes1 : votes2);
            string detailLine = "";
            if (!string.IsNullOrEmpty(failureReason))
                detailLine = Loc.T("poll.hud.result.failure", failureReason);
            else if (showTargetPlayer && !string.IsNullOrEmpty(_resultTargetPlayer))
                detailLine = Loc.T("poll.hud.result.target", _resultTargetPlayer);
            _hudResultText = Loc.T(
                "poll.hud.result",
                Loc.T("poll.hud.result.header"),
                resultLine,
                detailLine);
        }

        private void ClearOpenPoll()
        {
            _option1 = null;
            _option2 = null;
            _voteRemaining = 0f;
            _votes.Clear();
        }

        private void UpdatePollFile(float now, bool writePollFile)
        {
            if (!writePollFile || now - _lastFileUpdateTime < 1f)
                return;
            _lastFileUpdateTime = now;

            string content = "";
            if (IsOpen)
            {
                content = Loc.T(
                    "poll.file.open",
                    Math.Max(0, (int)Math.Ceiling(_voteRemaining)),
                    _option1.enemyName,
                    _votes.Count(vote => vote.Value == 1),
                    _option2.enemyName,
                    _votes.Count(vote => vote.Value == 2));
            }
            else if (_resultRemaining > 0f)
            {
                content = _resultText;
            }

            if (content == _lastFileText && File.Exists(_filePath))
                return;
            try
            {
                File.WriteAllText(_filePath, content);
                _lastFileText = content;
                _loggedFileError = false;
            }
            catch (Exception exception)
            {
                if (!_loggedFileError)
                {
                    _log("Could not update poll file: " + exception.Message);
                    _loggedFileError = true;
                }
            }
        }
    }
}
