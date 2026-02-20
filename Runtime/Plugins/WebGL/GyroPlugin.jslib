mergeInto(LibraryManager.library, {

  InitGyroscope: function () {
    window.unityGyroData = { alpha: 0, beta: 0, gamma: 0 };
    
    // Função interna de escuta
    function startListening() {
      window.addEventListener("deviceorientation", function(event) {
        window.unityGyroData.alpha = event.alpha;
        window.unityGyroData.beta = event.beta;
        window.unityGyroData.gamma = event.gamma;
      }, true);
    }

    // Verifica se é iOS 13+ (que requer permissão explícita)
    if (typeof DeviceOrientationEvent !== 'undefined' && typeof DeviceOrientationEvent.requestPermission === 'function') {
      DeviceOrientationEvent.requestPermission()
        .then(function(permissionState) {
          if (permissionState === 'granted') {
            startListening();
          } else {
            console.warn("Permissão do giroscópio negada pelo usuário.");
          }
        })
        .catch(function(error) {
          console.error("Erro ao pedir permissão:", error);
        });
    } else {
      // Dispositivos não-iOS ou antigos (Android, etc.)
      startListening();
    }
  },

  GetGyroAlpha: function () { return window.unityGyroData ? window.unityGyroData.alpha : 0; },
  GetGyroBeta: function () { return window.unityGyroData ? window.unityGyroData.beta : 0; },
  GetGyroGamma: function () { return window.unityGyroData ? window.unityGyroData.gamma : 0; }

});