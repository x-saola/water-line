using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Delta.Modules.WinStreak
{
    public class MilestonesAnimation : MonoBehaviour
    {
        private const int MILESTONE_STEP_3 = 3;
        private const int MILESTONE_STEP_5 = 5;
        private const int MILESTONE_STEP_7 = 7;
        private const int MILESTONE_STEP_10 = 10;
        private const float MILESTONE_SPACING = 155f;
        private const float ANIMATION_OFFSET_DISTANCE = 20f;
        private const float HIDE_DELAY_MULTIPLIER = 0.05f;
        private const float SHOW_DELAY_MULTIPLIER = 0.1f;
        private const float FAST_ANIMATION_DURATION_MULTIPLIER = 6f;
        private const float FAST_ANIMATION_DURATION = 0.2f;
        private const float SLOW_ANIMATION_DURATION = 0.5f;
        private const float NODE_ANIMATION_DELAY = 0.05f;
        private const float POST_ANIMATION_DELAY = 0.1f;
        private const float FAST_ANIMATION_COMPLETE_DELAY = 0.05f;
        private const float SLIDER_VALUE_STEP = 0.17f;

        [Header("UI References")]
        [SerializeField] private RectTransform _milestoneContainer;
        [SerializeField] private RectTransform _bgContainer;
        [SerializeField] private ParticleSystem _getMilestoneEffect;
        [SerializeField] private List<MilestoneUI> _milestones = new List<MilestoneUI>();
        [SerializeField] private Slider _progressBar;
        [SerializeField] private GameObject _popupLeaderboard;

        [Header("Animation Settings")]
        [SerializeField] private int _maxStepThreshold = 3;

        [Header("Debug")]
        [SerializeField] private int _testStep = 3;

        public Action OnAnimationComplete;
        public Action OnLeaderboardButtonClicked;
        public int currentMilestoneIndex
        {
            get => PlayerPrefs.GetInt("CurrentMilestoneIndex", 0);
        }

        private void SetCurrentMilestoneIndex(int index)
        {
            PlayerPrefs.SetInt("CurrentMilestoneIndex", index);
            PlayerPrefs.Save();
        }

        public void Init(int current = 0, bool isInstant = false)
        {
            HideAllGlows();
            SetCurrentMilestoneIndex(current);
            ResetContainerPositions();
            UpdateMilestones(true, false);
            UpdateProgressBar(Mathf.Min(current, 3), current, isInstant);
        }

        private void HideAllGlows()
        {
            for (int i = 0; i < _milestones.Count; i++)
            {
                _milestones[i].UpdateUI(_milestones[i].index, 0, false);
            }
        }

        [ContextMenu("Test Animation")]
        public void TestAnimation()
        {
            PlayMilestoneAnimation(currentMilestoneIndex, currentMilestoneIndex + _testStep);
        }

        public void UpdateMilestones(bool isInstant = false, bool animate = false)
        {
            int startIndex = Mathf.Max(0, currentMilestoneIndex - 3);
            ResetContainerPositions();

            for (int i = 0; i < _milestones.Count; i++)
            {
                int milestoneIndex = startIndex + i;
                _milestones[i].index = milestoneIndex;
                bool isShowReward = i < 6;
                ConfigureMilestone(_milestones[i], milestoneIndex, animate, isShowReward);
            }

            StartCoroutine(ShowMilestonesCoroutine(isInstant));
        }

        public void UpdateProgressBar(int current, int target, bool isInstant = false)
        {
            if (current > target)
            {
                Debug.Log("[MilestonesAnimation] UpdateProgressBar: current > target, skipping update.");
                return;
            }
            _progressBar.DOKill();
            Debug.Log($"[MilestonesAnimation] UpdateProgressBar: current={current}, target={target}");
            var currentMilestone = _milestones.FirstOrDefault(m => m.index == current);
            int currentIndex = _milestones.IndexOf(currentMilestone);
            _progressBar.value = Mathf.Min(3, Mathf.Max(0, currentIndex)) * SLIDER_VALUE_STEP;
            var targetMilestone = _milestones.FirstOrDefault(m => m.index == target);
            int index = _milestones.IndexOf(targetMilestone);
            float targetValue = Mathf.Min(3, Mathf.Max(0, index)) * SLIDER_VALUE_STEP;
            Debug.Log($"[MilestonesAnimation] UpdateProgressBar: currentIndex={currentIndex}, index={index}, targetValue={targetValue}");
            _progressBar.DOValue(targetValue, isInstant ? 0f : 0.5f).SetEase(Ease.InOutSine);
        }

        public void UpdateProgressBarByStepCount(int start, int stepCount)
        {
            _progressBar.DOKill();
            _progressBar.value = start * SLIDER_VALUE_STEP;
            float targetValue = start + stepCount * SLIDER_VALUE_STEP;
            _progressBar.DOValue(Mathf.Min(1, targetValue), 0.5f).SetEase(Ease.InOutSine);
        }

        public int CalculateTotalRewards(int stepCount)
        {
            int totalRewards = 0;

            for (int i = 0; i < stepCount; i++)
            {
                if (IsMilestoneIndex(currentMilestoneIndex + i))
                {
                    totalRewards++;
                }
            }

            return totalRewards;
        }

        public void PlayMilestoneAnimation(int current, int target, Action onComplete = null)
        {
            Debug.Log($"[MilestonesAnimation] Requested PlayMilestoneAnimation from {current} to {target} and currentMilestoneIndex is {currentMilestoneIndex}");
            if (current == target || target == currentMilestoneIndex)
            {
                // When reopening the popup, callers often pass values that match the persisted value.
                // In that case we still must force-refresh the UI (milestones/progressbar), otherwise
                // the popup can show stale state on first open.
                Debug.Log($"[MilestonesAnimation] PlayMilestoneAnimation early-refresh (current={current}, target={target}, persisted={currentMilestoneIndex})");
                StopAllCoroutines();
                SetCurrentMilestoneIndex(target);
                Init(target, true);
                onComplete?.Invoke();
                return;
            }
            if (current < currentMilestoneIndex)
            {
                current = currentMilestoneIndex;
                Debug.Log($"[MilestonesAnimation] Adjusted current to persisted value: {current}");
            }
            Debug.Log($"[MilestonesAnimation] PlayMilestoneAnimation from {current} to {target}");
            StopAllCoroutines();
            SetCurrentMilestoneIndex(current);
            OnAnimationComplete = onComplete;
            int stepCount = target - current;
            if (stepCount > _maxStepThreshold)
            {
                StartCoroutine(PlayFastAnimationCoroutine(target));
            }
            else
            {
                StartCoroutine(PlaySlowAnimationCoroutine(current, target));
            }
        }

        private bool IsMilestoneIndex(int index)
        {
            if (index == 0) return false;

            int remainder = index % MILESTONE_STEP_10;
            return remainder == MILESTONE_STEP_3 ||
                   remainder == MILESTONE_STEP_5 ||
                   remainder == MILESTONE_STEP_7 ||
                   remainder == 0;
        }

        private bool IsReverseMilestone(int index)
        {
            int remainder = index % MILESTONE_STEP_10;
            return remainder == 0 || remainder == MILESTONE_STEP_5;
        }

        private void ConfigureMilestone(MilestoneUI milestone, int index, bool animate = true, bool showReward = true)
        {
            milestone.isActive = IsMilestoneIndex(index);
            milestone.isReverseMilestone = IsReverseMilestone(index);
            milestone.UpdateUI(index, currentMilestoneIndex, animate, showReward);
        }

        private void ResetContainerPositions()
        {
            _milestoneContainer.anchoredPosition = Vector2.zero;
            _bgContainer.anchoredPosition = Vector2.zero;
        }

        private IEnumerator PlaySlowAnimationCoroutine(int current, int target)
        {
            int step = target > 3 ? Mathf.Max(0, target - current) : 0;

            UpdateMilestones(true, false);

            yield return new WaitForSeconds(0.5f);

            float targetY = _milestoneContainer.anchoredPosition.y + step * MILESTONE_SPACING;
            float totalDuration = SLOW_ANIMATION_DURATION * step;

            AnimateContainers(targetY, totalDuration, Ease.Linear);
            for (int i = 0; i < step; i++)
            {
                HideMilestoneOutOfView(_milestones[i]);
            }
            int index = Mathf.Min(current, 3);
            if (current <= 3)
            {
                UpdateProgressBar(current, target);
            }
            else
            {
                UpdateProgressBar(current - 2, target);
            }
            int stepCount = target - current;
            float stepDelay = stepCount > 0 ? SLOW_ANIMATION_DURATION : 0f;

            for (int i = current; i <= target; i++)
            {
                _milestones[index].UpdateUI(i, target, true);
                index++;
                yield return new WaitForSeconds(stepDelay);
            }
            OnAnimationComplete?.Invoke();
            SetCurrentMilestoneIndex(target);
        }

        private void HideMilestoneOutOfView(MilestoneUI milestone)
        {
            milestone.HideText();
            milestone.ShowMilestoneReward(false);
        }

        private IEnumerator PlayFastAnimationCoroutine(int target)
        {
            yield return HideAllMilestonesCoroutine();

            float targetY = _milestoneContainer.anchoredPosition.y + ANIMATION_OFFSET_DISTANCE * MILESTONE_SPACING;
            float duration = FAST_ANIMATION_DURATION * FAST_ANIMATION_DURATION_MULTIPLIER;
            UpdateProgressBarByStepCount(0, 10);
            yield return StartCoroutine(FakeActiveAllMilestones());
            AnimateContainers(targetY, duration, Ease.InOutSine);

            yield return new WaitForSeconds(duration);
            UpdateProgressBarByStepCount(0, 3);

            ResetContainerPositions();
            SetCurrentMilestoneIndex(target);
            OnAnimationComplete?.Invoke();
            UpdateMilestones(true, false);

            yield return new WaitForSeconds(FAST_ANIMATION_COMPLETE_DELAY);
            StartCoroutine(PlayAnimationLoadNodeActive());
        }

        private IEnumerator FakeActiveAllMilestones()
        {
            for (int i = 0; i < _milestones.Count; i++)
            {
                _milestones[i].ForceNodeImage(true);
                yield return new WaitForSeconds(i < 7 ? 0.01f : 0f);
            }
        }

        private IEnumerator PlayAnimationLoadNodeActive()
        {
            int count = Mathf.Min(currentMilestoneIndex, 4);

            for (int i = 0; i < count; i++)
            {
                _milestones[i].HideGlowTemporarily();
            }

            for (int i = 0; i < count; i++)
            {
                _milestones[i].AnimateDisplay(currentMilestoneIndex);
                yield return new WaitForSeconds(NODE_ANIMATION_DELAY);
            }
        }

        private IEnumerator HideAllMilestonesCoroutine()
        {
            for (int i = 0; i < _milestones.Count; i++)
            {
                _milestones[i].ShowMilestoneReward(false);
                _milestones[i].HideText();
                yield return new WaitForSeconds(HIDE_DELAY_MULTIPLIER / (i + 1));
            }
        }

        private IEnumerator ShowMilestonesCoroutine(bool isInstant = false)
        {
            for (int i = 0; i < _milestones.Count; i++)
            {
                MilestoneUI milestone = _milestones[i];
                bool shouldShow = IsMilestoneIndex(milestone.index);

                milestone.isActive = shouldShow;
                milestone.ShowMilestoneReward(shouldShow);
                milestone.UpdateUI(milestone.index, currentMilestoneIndex, false);

                if (!isInstant)
                {
                    yield return new WaitForSeconds(SHOW_DELAY_MULTIPLIER / (i + 1));
                }
            }
        }

        private void AnimateContainers(float targetY, float duration, Ease ease)
        {
            _milestoneContainer.DOAnchorPosY(targetY, duration).SetEase(ease);
            _bgContainer.DOAnchorPosY(targetY, duration).SetEase(ease);
        }

        public void OnLeaderboardButtonClickedHandler()
        {
            _popupLeaderboard.SetActive(true);
            OnLeaderboardButtonClicked?.Invoke();
        }
        public void CloseLeaderboardPopup()
        {
            _popupLeaderboard.SetActive(false);
        }
    }
}
