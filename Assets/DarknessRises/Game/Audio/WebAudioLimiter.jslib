// The Web player's safety limiter (issue #121). Unity's Web audio connects every sound to
// WEBAudio.audioContext.destination each time it plays, with nothing between. Here the context's `destination` is
// shadowed by a hard-knee DynamicsCompressorNode (the limiter) followed by a gain that takes the compressor's automatic
// makeup gain back off; those feed the real output. Sounds under the threshold play exactly as before.
mergeInto(LibraryManager.library, {
  DarknessRisesAudioLimiterInstall__deps: ['$WEBAudio'],
  DarknessRisesAudioLimiterInstall: function (thresholdDecibels, ratio, attackSeconds, releaseSeconds, compensationGain) {
    try {
      var context = WEBAudio.audioContext;
      if (!context) {
        console.warn('[Audio] No Web audio context yet: the output is not limited.');
        return 0;
      }

      if (context.darknessRisesLimiter) {
        return 1;
      }

      var output = context.destination;
      var limiter = context.createDynamicsCompressor();
      limiter.threshold.value = thresholdDecibels;
      limiter.knee.value = 0;
      limiter.ratio.value = ratio;
      limiter.attack.value = attackSeconds;
      limiter.release.value = releaseSeconds;
      var level = context.createGain();
      level.gain.value = compensationGain;
      limiter.connect(level);
      level.connect(output);
      Object.defineProperty(context, 'destination', { value: limiter, configurable: true });
      context.darknessRisesLimiter = limiter;
      return 1;
    } catch (error) {
      console.warn('[Audio] The Web limiter could not be installed: ' + error);
      return 0;
    }
  },
});
