https://athena-studio.atlassian.net/wiki/spaces/ST/pages/2823454777/ATT+Package

1.Setup config in ATTSettings.cs
2.Usage

AppTrackingTransparency.RequestTrackingAuthorization(callback)

Example:
	AppTrackingTransparency.RequestTrackingAuthorization((status) => {
        if (status == AppTrackingTransparency.AuthorizationStatus.Authorized) {
            Debug.Log("Authorized!");
        } else 
        {
            Debug.Log("Do something!");
        }
    });

WARNING: callback is not call on main thread. So just use it to setting flag
then use Monobehaviour.Update() to process or run a coroutine from a Monobehaviour.
