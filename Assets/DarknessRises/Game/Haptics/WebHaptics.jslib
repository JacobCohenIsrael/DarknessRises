// Haptics on the Web player (issue #130): navigator.vibrate. The game hands every pattern over once, by index; playing
// one is a lookup and a call. Chrome on Android vibrates; iOS Safari has no navigator.vibrate; desktop browsers have it
// but no motor, so support also asks for a touch screen. The browser ignores vibrate until the page (or, embedded as on
// itch.io, the game's own frame) has been tapped: until then it's skipped quietly rather than logged as an intervention.
mergeInto(LibraryManager.library, {
  DarknessRisesHapticsSupported: function () {
    try {
      if (typeof navigator === 'undefined' || typeof navigator.vibrate !== 'function') {
        return 0;
      }

      var touch = (navigator.maxTouchPoints || 0) > 0
        || (typeof matchMedia === 'function' && matchMedia('(pointer: coarse)').matches);
      return touch ? 1 : 0;
    } catch (error) {
      return 0;
    }
  },

  DarknessRisesHapticsRegister: function (index, pattern, length) {
    var patterns = Module.darknessRisesHaptics || (Module.darknessRisesHaptics = []);
    var copy = [];
    for (var i = 0; i < length; i++) {
      copy.push(HEAP32[(pattern >> 2) + i]);
    }

    patterns[index] = copy;
  },

  DarknessRisesHapticsPlay: function (index) {
    try {
      var patterns = Module.darknessRisesHaptics;
      var pattern = patterns && patterns[index];
      if (!pattern || pattern.length === 0) {
        return 0;
      }

      if (navigator.userActivation && !navigator.userActivation.hasBeenActive) {
        return 0;
      }

      return navigator.vibrate(pattern) ? 1 : 0;
    } catch (error) {
      return 0;
    }
  },

  DarknessRisesHapticsCancel: function () {
    try {
      if (navigator.userActivation && !navigator.userActivation.hasBeenActive) {
        return;
      }

      navigator.vibrate(0);
    } catch (error) {
    }
  },
});
