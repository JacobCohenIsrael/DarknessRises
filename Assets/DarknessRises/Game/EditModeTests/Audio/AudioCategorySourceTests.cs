using DarknessRises.Game.Audio;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Audio
{
    public sealed class AudioCategorySourceTests
    {
        private const float Tolerance = 1e-5f;
        private GameObject sourceObject;

        [TearDown]
        public void DestroySource()
        {
            if (sourceObject != null)
            {
                Object.DestroyImmediate(sourceObject);
            }
        }

        [Test]
        public void A_view_sets_its_own_volume_and_the_category_scales_it()
        {
            sourceObject = new GameObject("Source", typeof(AudioSource));
            var categorized = AudioCategorySource.Add(sourceObject, AudioCategory.Ambient);

            categorized.Source.SetVolume(0.4f);

            Assert.That(categorized.Category, Is.EqualTo(AudioCategory.Ambient));
            Assert.That(categorized.Volume, Is.EqualTo(0.4f).Within(Tolerance));

            // No scene volumes here: the gain is 1, so the source plays the view's volume.
            Assert.That(categorized.Source.volume, Is.EqualTo(0.4f * categorized.Gain).Within(Tolerance));
        }

        [Test]
        public void A_source_keeps_the_volume_it_was_built_with_until_a_view_sets_one()
        {
            sourceObject = new GameObject("Source", typeof(AudioSource));
            sourceObject.GetComponent<AudioSource>().volume = 0.3f;

            var categorized = AudioCategorySource.Add(sourceObject, AudioCategory.Sound);

            Assert.That(categorized.Volume, Is.EqualTo(0.3f).Within(Tolerance));
        }

        [Test]
        public void A_source_without_a_category_takes_the_volume_as_it_is()
        {
            sourceObject = new GameObject("Source", typeof(AudioSource));
            var source = sourceObject.GetComponent<AudioSource>();

            source.SetVolume(0.6f);

            Assert.That(source.volume, Is.EqualTo(0.6f).Within(Tolerance));
        }

        [Test]
        public void Setting_a_volume_every_frame_allocates_nothing()
        {
            sourceObject = new GameObject("Source", typeof(AudioSource));
            var source = AudioCategorySource.Add(sourceObject, AudioCategory.Sound).Source;
            var volume = 0f;

            AllocationAssert.DoesNotAllocate(() =>
            {
                volume = (volume + 0.1f) % 1f;
                source.SetVolume(volume);
            });
        }
    }
}
