#if USE_ADMOB_MEDIATION
using GoogleMobileAds.Ump.Api;
using UnityEngine;
using System;
using GoogleMobileAds.Api;

namespace Delta.GameOps
{
    public class GoogleCMPManager : MonoBehaviour
    {
        private bool isOptInConsent = false;
        public bool IsOptInConsent
        {
            get { return isOptInConsent; }
        }
        [HideInInspector]
        public bool IsShowButtonPrivacy = false;
        // Start is called before the first frame update
        void Start()
        {
            try
            {
                InitCMPFlow();
            }
            catch (Exception ex)
            {
                Debug.Log("[cmp] Exception:  " + ex.Message);
                // InitAthenaService();
            }
        }
        void InitCMPFlow()
        {
            // #if CHEAT
            //         var debugSettings = new ConsentDebugSettings
            //         {
            //             // Geography appears as in EEA for debug devices.
            //             DebugGeography = DebugGeography.EEA,
            //             TestDeviceHashedIds = new List<string>
            //             {
            //                 "2983CBF571B1802CBCA1452A6C30E443"
            //             }
            //         };
            //         // Set tag for under age of consent.
            //         // Here false means users are not under age of consent.
            //         ConsentRequestParameters request = new ConsentRequestParameters
            //         {
            //             TagForUnderAgeOfConsent = false,
            //             ConsentDebugSettings = debugSettings,
            //         };
            // #else
            // Set tag for under age of consent.
            // Here false means users are not under age of consent.
            ConsentRequestParameters request = new ConsentRequestParameters
            {
                TagForUnderAgeOfConsent = false
            };
            // #endif
            // Check the current consent information status.
            ConsentInformation.Update(request, OnConsentInfoUpdated);
        }
        public void ResetCMPToTest()
        {
#if CHEAT
            ConsentInformation.Reset();
#endif
        }
        void OnConsentInfoUpdated(FormError consentError)
        {
            if (consentError != null)
            {
                Debug.Log("[cmp] " + consentError);
            }
            else
            {
                // If the error is null, the consent information state was updated.
                // You are now ready to check if a form is available.
                ConsentForm.LoadAndShowConsentFormIfRequired((FormError formError) =>
                {
                    if (formError != null)
                    {
                        Debug.Log("[cmp] " + formError);
                    }
                    else
                    {
                        // Consent has been gathered.
                        if (ConsentInformation.CanRequestAds())
                        {
                            isOptInConsent = true;
                        }
                        if (ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required)
                        {
                            IsShowButtonPrivacy = true;
                        }
                        bool isEEA = IsShowButtonPrivacy ? true : false;
                        bool is_ad_personalization = true;
                        bool is_ad_user_data = true;
                        if (isEEA)
                        {
                            // Example value: "1111111111"
                            string purposeConsents = ApplicationPreferences.GetString("IABTCF_PurposeConsents");
                            Debug.Log("[google_cmp]  purposeConsents " + purposeConsents);

                            // Purposes are zero-indexed. Index 0 contains information about Purpose 1.
                            bool purpose_1 = true;
                            bool purpose_3 = true;
                            bool purpose_4 = true;
                            bool purpose_7 = true;
                            try
                            {
                                if (!string.IsNullOrEmpty(purposeConsents))
                                {
                                    char purpose_1_String = '0', purpose_3_String = '0', purpose_4_String = '0', purpose_7_String = '0';

                                    if (purposeConsents.Length > 0) purpose_1_String = purposeConsents[0];
                                    if (purposeConsents.Length > 2) purpose_3_String = purposeConsents[2];
                                    if (purposeConsents.Length > 3) purpose_4_String = purposeConsents[3];
                                    if (purposeConsents.Length > 6) purpose_7_String = purposeConsents[6];

                                    purpose_1 = purpose_1_String == '1';
                                    purpose_3 = purpose_3_String == '1';
                                    purpose_4 = purpose_4_String == '1';
                                    purpose_7 = purpose_7_String == '1';
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError("[google_cmp] error parse " + ex.Message);
                            }
                            if (purpose_1 == false || purpose_7 == false)
                            {
                                is_ad_user_data = false;
                            }
                            if (purpose_3 == false || purpose_4 == false)
                            {
                                is_ad_personalization = false;
                            }

                            Debug.Log("[google_cmp]  is_ad_personalization " + is_ad_personalization);
                            Debug.Log("[google_cmp]  is_ad_user_data " + is_ad_user_data);
                        }
#if USE_ADJUST
                        string eea = IsShowButtonPrivacy ? "1" : "0";
                        string ad_personalization = is_ad_personalization ? "1" : "0";
                        string ad_user_data = is_ad_user_data ? "1" : "0";
                        DeltaApp.Instance.SetupAdjustGoogleCMP(eea, ad_personalization, ad_user_data);
#endif
                    }
                });
            }
        }

        public void ShowPrivacyForm()
        {
            ConsentForm.ShowPrivacyOptionsForm((FormError err) =>
            {
                Debug.Log("[cmp]ShowPrivacyOptionsForm " + err);
            });
        }

    }
}
#endif