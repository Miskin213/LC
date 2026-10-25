using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace PalaCompany
{
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
        private readonly Action<string, string> _showTip;
        private readonly string _filePath;
        private readonly System.Random _random = new System.Random();
        private readonly Dictionary<string, int> _votes =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private EnemyType _option1;
        private EnemyType _option2;
        private float _voteRemaining;
        private float _cycleRemaining;
        private float _resultRemaining;
        private float _lastUpdateTime;
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
        private string _lastFileText;

        internal Poll(
            Action<string> log,
            Func<IList<EnemyType>> getCandidates,
            Func<EnemyType, PollSpawnResult> trySpawn,
            Action<string, string> showTip,
            string filePath)
        {
            _log = log;
            _getCandidates = getCandidates;
            _trySpawn = trySpawn;
            _showTip = showTip;
            _filePath = filePath;
        }

        internal bool IsOpen => _option1 != null && _option2 != null;

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
                if (IsOpen)
                {
                    _log("Poll cancelled because the host is no longer in a landed round.");
                    ClearOpenPoll();
                    _debugForced = false;
                }
                UpdatePollFile(now, writePollFile);
                return;
            }

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
            _pollsOpenedThisRound++;
            if (maxPollsPerRound > 0 && _pollsOpenedThisRound >= maxPollsPerRound)
                LogPollLimitReached();
            _voteRemaining = Math.Max(1f, voteSeconds);
            _resultRemaining = 0f;
            _resultText = "";
            _showTip(
                Loc.T("poll.open.header"),
                Loc.T("poll.open.body", _option1.enemyName, _option2.enemyName));
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
                _option1.enemyName,
                votes1,
                _option2.enemyName,
                votes2);
            if (winner == null)
            {
                _resultText = Loc.T("poll.result.file.skipped");
                _log("Poll closed with no votes; skipped.");
            }
            else
            {
                _resultText = Loc.T("poll.result.file.winner", winner.enemyName, voteSummary);
            }

            ClearOpenPoll();
            _resultRemaining = 10f;
            _cycleRemaining = Math.Max(1f, cycleSeconds);
            _cycleInitialized = true;
            _debugForced = false;

            if (winner == null)
            {
                _showTip(
                    Loc.T("poll.result.tip.skipped.header"),
                    Loc.T("poll.result.tip.skipped.body", voteSummary));
                return;
            }

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

            string resultBody = voteSummary;
            if (!string.IsNullOrEmpty(failureReason))
                resultBody += Loc.T("poll.spawn.failed", winner.enemyName, failureReason);
            string resultHeader = showTargetPlayer
                && spawnResult != null
                && spawnResult.Spawned
                && !string.IsNullOrEmpty(spawnResult.TargetPlayerName)
                    ? Loc.T("tip.spawn.header", winner.enemyName + " spawned near " + spawnResult.TargetPlayerName)
                    : Loc.T("poll.result.tip.winner.header", winner.enemyName);
            _showTip(resultHeader, resultBody);
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
