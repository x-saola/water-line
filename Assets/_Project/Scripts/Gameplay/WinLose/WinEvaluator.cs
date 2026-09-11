using System;
using System.Collections.Generic;

namespace Delta.ProjectName
{
    public class WinEvaluationResult
    {
        public bool IsWin { get; }
        public IReadOnlyDictionary<Type, bool> RuleResults { get; }

        public WinEvaluationResult(bool isWin, IReadOnlyDictionary<Type, bool> ruleResults)
        {
            IsWin = isWin;
            RuleResults = ruleResults;
        }
    }

    public class WinEvaluator
    {
        readonly List<IWinRule> _rules;

        public WinEvaluator(IEnumerable<IWinRule> rules = null)
        {
            _rules = rules != null
                ? new List<IWinRule>(rules)
                : new List<IWinRule> { new AllPipesMeetRule(), new AllValvesOpenRule() };
        }

        // Runs every rule - never short-circuits - so each is independently inspectable/
        // assertable rather than folded into one opaque boolean expression.
        public WinEvaluationResult Evaluate(GameplaySessionState state)
        {
            var results = new Dictionary<Type, bool>();
            bool allSatisfied = true;

            foreach (var rule in _rules)
            {
                bool satisfied = rule.IsSatisfied(state);
                results[rule.GetType()] = satisfied;
                allSatisfied &= satisfied;
            }

            return new WinEvaluationResult(allSatisfied, results);
        }
    }
}
