using System;
using System.Collections.Generic;

namespace Delta.ProjectName
{
    public class LoseEvaluationResult
    {
        public bool IsLose { get; }
        public IReadOnlyDictionary<Type, bool> RuleResults { get; }

        public LoseEvaluationResult(bool isLose, IReadOnlyDictionary<Type, bool> ruleResults)
        {
            IsLose = isLose;
            RuleResults = ruleResults;
        }
    }

    public class LoseEvaluator
    {
        readonly List<ILoseRule> _rules;

        public LoseEvaluator(IEnumerable<ILoseRule> rules = null)
        {
            _rules = rules != null ? new List<ILoseRule>(rules) : new List<ILoseRule> { new TimeExpiredRule() };
        }

        // Same shape as WinEvaluator, runs every rule without short-circuiting. Unlike win
        // (needs ALL rules satisfied), lose is true if ANY rule is satisfied - there can be
        // multiple independent fail conditions in general even though MVP only ships one.
        public LoseEvaluationResult Evaluate(GameplaySessionState state)
        {
            var results = new Dictionary<Type, bool>();
            bool anySatisfied = false;

            foreach (var rule in _rules)
            {
                bool satisfied = rule.IsSatisfied(state);
                results[rule.GetType()] = satisfied;
                anySatisfied |= satisfied;
            }

            return new LoseEvaluationResult(anySatisfied, results);
        }
    }
}
