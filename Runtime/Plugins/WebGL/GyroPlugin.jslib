mergeInto(LibraryManager.library, {
  InitGyroscope: function () {
    if (!window.unityGyroData) {
      window.unityGyroData = { alpha: 0, beta: 0, gamma: 0 };
    }

    if (!window.unityGyroStatus) {
      window.unityGyroStatus = {
        hasEvent: false,
        listenerAdded: false,
        permissionState: 0
      };
    }

    if (!window.unityGyroRuntime) {
      window.unityGyroRuntime = {
        lastMotionTs: 0
      };
    }

    var status = window.unityGyroStatus;
    var runtime = window.unityGyroRuntime;

    function onOrientation(event) {
      if (!event) return;
      if (event.alpha == null && event.beta == null && event.gamma == null) return;

      window.unityGyroData.alpha = event.alpha != null ? event.alpha : 0;
      window.unityGyroData.beta = event.beta != null ? event.beta : 0;
      window.unityGyroData.gamma = event.gamma != null ? event.gamma : 0;
      status.hasEvent = true;
    }

    // Android fallback: integrate angular velocity when deviceorientation is missing.
    function onMotion(event) {
      if (!event || !event.rotationRate) return;

      var rr = event.rotationRate;
      if (rr.alpha == null && rr.beta == null && rr.gamma == null) return;

      var nowTs = event.timeStamp || Date.now();
      var dt = runtime.lastMotionTs > 0 ? (nowTs - runtime.lastMotionTs) / 1000.0 : 0.016;
      runtime.lastMotionTs = nowTs;

      if (dt < 0.0) dt = 0.0;
      if (dt > 0.2) dt = 0.2;

      window.unityGyroData.alpha += (rr.alpha != null ? rr.alpha : 0) * dt;
      window.unityGyroData.beta += (rr.beta != null ? rr.beta : 0) * dt;
      window.unityGyroData.gamma += (rr.gamma != null ? rr.gamma : 0) * dt;
      status.hasEvent = true;
    }

    function startListening() {
      if (status.listenerAdded) return;

      window.addEventListener("deviceorientation", onOrientation, true);
      window.addEventListener("devicemotion", onMotion, true);
      status.listenerAdded = true;
      status.permissionState = 1;
    }

    if (typeof window.DeviceOrientationEvent === "undefined") {
      status.permissionState = 3;
      console.warn("DeviceOrientationEvent is not supported in this browser.");
      return;
    }

    // iOS 13+ path: explicit permission is required.
    if (typeof DeviceOrientationEvent.requestPermission === "function") {
      DeviceOrientationEvent.requestPermission()
        .then(function (permissionState) {
          if (permissionState === "granted") {
            startListening();
          } else {
            status.permissionState = 2;
            console.warn("Gyroscope permission denied by user.");
          }
        })
        .catch(function (error) {
          status.permissionState = 4;
          console.error("Error requesting gyroscope permission:", error);
        });
    } else {
      // Android and most non-iOS browsers.
      startListening();
    }
  },

  GetGyroAlpha: function () {
    return window.unityGyroData ? window.unityGyroData.alpha : 0;
  },

  GetGyroBeta: function () {
    return window.unityGyroData ? window.unityGyroData.beta : 0;
  },

  GetGyroGamma: function () {
    return window.unityGyroData ? window.unityGyroData.gamma : 0;
  },

  GetGyroHasData: function () {
    if (!window.unityGyroStatus || !window.unityGyroStatus.hasEvent) return 0;
    return 1;
  },

  GetGyroPermissionState: function () {
    return window.unityGyroStatus ? window.unityGyroStatus.permissionState : 0;
  }
});
