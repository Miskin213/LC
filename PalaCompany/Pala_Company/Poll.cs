using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace PalaCompany
{
    internal sealed class Poll
    {
        private readonly Action<string> _log;
        private readonly Func<IList<EnemyType>> _getCandidates;
        private readonly Func<EnemyType, string> _trySpawn;
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
        private bool _loggedFileError;
        private string _resultText = "";
        private string _lastFileText;

        internal Poll(
            Action<string> log,
            Func<IList<EnemyType>> getCandidates,
            Func<EnemyType, string> trySpawn,
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
            float cycleSeconds,
            float voteSeconds,
            string noVotesBehavior,
            bool writePollFile,
            bool forceToggle)
        {
            float now = Time.time;
            float elapsed = _hasUpdatedTime ? Math.Max(0f, now - _lastUpdateTime) : 0f;
            _lastUpdateTime = now;
            _hasUpdatedTime = true;

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
                    ClosePoll(cycleSeconds, noVotesBehavior);
                else
                {
                    _debugForced = true;
                    if (!OpenPoll(voteSeconds))
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
                    ClosePoll(cycleSeconds, noVotesBehavior);
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
                        if (!OpenPoll(voteSeconds))
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

        private bool OpenPoll(float voteSeconds)
        {
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
            _voteRemaining = Math.Max(1f, voteSeconds);
            _resultRemaining = 0f;
            _resultText = "";
            _showTip(
                "POLL: type !1 or !2",
                "1) " + _option1.enemyName + "\n2) " + _option2.enemyName);
            return true;
        }

        private void ClosePoll(float cycleSeconds, string noVotesBehavior)
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

            string voteSummary = "1) " + _option1.enemyName + " - " + votes1 + " votes; "
                + "2) " + _option2.enemyName + " - " + votes2 + " votes";
            if (winner == null)
            {
                _resultText = "POLL RESULT - skipped (no votes)";
                _log("Poll closed with no votes; skipped.");
            }
            else
            {
                _resultText = "POLL RESULT - " + winner.enemyName + " won; " + voteSummary;
            }

            ClearOpenPoll();
            _resultRemaining = 10f;
            _cycleRemaining = Math.Max(1f, cycleSeconds);
            _cycleInitialized = true;
            _debugForced = false;

            if (winner == null)
            {
                _showTip("POLL: skipped", "No votes; poll skipped. " + voteSummary);
                return;
            }

            string failureReason = _trySpawn(winner);
            if (failureReason == string.Empty)
                failureReason = "spawn failed; see the plugin log for details";
            if (!string.IsNullOrEmpty(failureReason))
                _log("Poll spawn failed: " + failureReason);

            string resultBody = voteSummary;
            if (!string.IsNullOrEmpty(failureReason))
                resultBody += "\nCouldn't spawn " + winner.enemyName + ": " + failureReason;
            _showTip("POLL: " + winner.enemyName + " won", resultBody);
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
                content = "POLL - " + Math.Max(0, (int)Math.Ceiling(_voteRemaining)) + "s left\n"
                    + "1) " + _option1.enemyName + " - " + _votes.Count(vote => vote.Value == 1) + " votes\n"
                    + "2) " + _option2.enemyName + " - " + _votes.Count(vote => vote.Value == 2) + " votes";
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
