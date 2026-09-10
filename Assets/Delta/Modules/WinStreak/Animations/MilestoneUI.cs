using System.Collections;
using Delta.ConnectMaster;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Delta.Modules.WinStreak
{
    public class MilestoneUI : MonoBehaviour
    {
        private const float SCALE_MULTIPLIER = 1.4f;
        private const float SCALE_DURATION = 0.2f;
        private const float SCALE_ANIMATION_DURATION = 0.3f;

        [Header("Milestone Components")]
        [SerializeField] private MilestoneBox _milestoneReward;
        [SerializeField] private MilestoneBox _milestoneRewardReverse;
        [SerializeField] private TMP_Text _milestoneIndexText;

        [Header("Visual Effects")]
        [SerializeField] private Image _glowActive;
        [SerializeField] private Image _glowCurrent;
        [SerializeField] private Image _nodeImage;
        [SerializeField] private ParticleSystem _activeEffect;

        [Header("Node Sprites")]
        [SerializeField] private Sprite _nodeActive;
        [SerializeField] private Sprite _nodeDeactive;

        public bool isActive;
        public int index;
        public bool isReverseMilestone;

        public void UpdateText()
        {
            _milestoneIndexText.text = index.ToString();
        }

        public void ShowMilestoneReward(bool show)
        {
            UpdateText();
            ShowText();

            if (!show)
            {
                StartCoroutine(HideMilestoneRewardCoroutine());
                return;
            }

            StartCoroutine(ShowMilestoneRewardCoroutine());
        }


        public void AnimateDisplay(int currentMilestoneIndex)
        {
            transform.localScale = Vector3.one;
            transform.DOKill();
            _activeEffect.Play();

            transform.DOScale(Vector3.one * SCALE_MULTIPLIER, SCALE_DURATION)
                .SetEase(Ease.InSine)
                .SetLoops(2, LoopType.Yoyo)
                .OnComplete(() =>
                {
                    transform.localScale = Vector3.one;
                    SetStateLocked(index > currentMilestoneIndex);
                    ShowGlowAgain();
                });
        }

        public void HideGlowTemporarily()
        {
            SetGlowEnabled(false);
        }

        public void ShowGlowAgain()
        {
            SetGlowEnabled(true);
        }

        public void UpdateUI(int index, int currentMilestoneIndex, bool animate = false, bool showReward = true)
        {
            this.index = index;
            UpdateText();
            UpdateGlow(currentMilestoneIndex);
            UpdateNodeImage(currentMilestoneIndex);
            if (showReward)
            {
                StartCoroutine(ShowMilestoneRewardCoroutine());
            }
            else
            {
                _milestoneReward.gameObject.SetActive(false);
                _milestoneRewardReverse.gameObject.SetActive(false);
            }

            if (animate)
            {
                AnimateDisplay(currentMilestoneIndex);
            }
            else
            {
                SetStateLocked(index > currentMilestoneIndex);
            }
        }

        public void UpdateGlow(int currentMilestoneIndex)
        {
            bool isPassed = index < currentMilestoneIndex;
            bool isCurrent = index == currentMilestoneIndex;

            _glowActive.gameObject.SetActive(isPassed);
            _glowActive.enabled = isPassed;

            _glowCurrent.gameObject.SetActive(isCurrent);
            _glowCurrent.enabled = isCurrent;
        }

        private void SetGlowEnabled(bool enabled)
        {
            _glowActive.enabled = enabled;
            _glowCurrent.enabled = enabled;
        }

        public void ForceNodeImage(bool isShow)
        {
            _nodeImage.sprite = isShow ? _nodeActive : _nodeDeactive;
        }

        private void UpdateNodeImage(int current)
        {
            _nodeImage.sprite = index <= current ? _nodeActive : _nodeDeactive;
        }

        public void HideText()
        {
            _milestoneIndexText.transform.localScale = Vector3.one;
            _milestoneIndexText.transform.DOScale(Vector3.zero, SCALE_DURATION).SetEase(Ease.InBack);
        }

        public void ShowText()
        {
            _milestoneIndexText.transform.localScale = Vector3.zero;
            _milestoneIndexText.transform.DOScale(Vector3.one, SCALE_DURATION).SetEase(Ease.OutBack);
        }

        private IEnumerator ShowMilestoneRewardCoroutine()
        {
            ShowText();

            if (!isActive)
            {
                HideAllRewardBoxes();
                Debug.Log("[MilestoneUI] Milestone is not active, skipping reward show.");
                yield break;
            }

            MilestoneBox targetReward = isReverseMilestone ? _milestoneRewardReverse : _milestoneReward;
            MilestoneBox otherReward = isReverseMilestone ? _milestoneReward : _milestoneRewardReverse;

            otherReward.gameObject.SetActive(false);
            targetReward.transform.localScale = Vector3.zero;
            targetReward.gameObject.SetActive(true);

            yield return targetReward.transform
                .DOScale(Vector3.one, SCALE_ANIMATION_DURATION)
                .SetEase(Ease.OutBack)
                .WaitForCompletion();
        }

        public void SetStateLocked(bool isLocked)
        {
            MilestoneBox targetReward = isReverseMilestone ? _milestoneRewardReverse : _milestoneReward;
            targetReward.SetLockedState(isLocked);
        }

        private IEnumerator HideMilestoneRewardCoroutine()
        {
            yield return null;

            _milestoneReward.transform.localScale = Vector3.one;
            yield return _milestoneReward.transform
                .DOScale(Vector3.zero, SCALE_DURATION)
                .SetEase(Ease.InBack)
                .OnComplete(HideAllRewardBoxes)
                .WaitForCompletion();
        }

        private void HideAllRewardBoxes()
        {
            _milestoneReward.gameObject.SetActive(false);
            _milestoneRewardReverse.gameObject.SetActive(false);
        }
    }
}
