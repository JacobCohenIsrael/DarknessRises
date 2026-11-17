using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DarknessRises.Game.Content;
using NUnit.Framework;
using UnityEngine.SceneManagement;

namespace DarknessRises.Game.EditModeTests.Content
{
    public sealed class ContentScopeTests
    {
        private FakeSource source;
        private ContentScope scope;

        [SetUp]
        public void SetUp()
        {
            source = new FakeSource();
            scope = new ContentScope(source);
        }

        [Test]
        public void Loads_an_asset_from_its_source()
        {
            var load = scope.LoadAssetAsync<string>("shambler", CancellationToken.None);
            source.Finish("shambler", "Shambler prefab");

            Assert.That(Result(load), Is.EqualTo("Shambler prefab"));
            Assert.That(scope.Get<string>("shambler"), Is.EqualTo("Shambler prefab"));
        }

        [Test]
        public void Loads_each_asset_once_however_often_it_is_asked_for()
        {
            var first = scope.LoadAssetAsync<string>("shot", CancellationToken.None);
            var second = scope.LoadAssetAsync<string>("shot", CancellationToken.None);
            source.Finish("shot", "Shot prefab");
            var third = scope.LoadAssetAsync<string>("shot", CancellationToken.None);

            Assert.That(Result(first), Is.EqualTo("Shot prefab"));
            Assert.That(Result(second), Is.EqualTo("Shot prefab"));
            Assert.That(Result(third), Is.EqualTo("Shot prefab"));
            Assert.That(source.Started, Is.EqualTo(new[] { "shot" }));
            Assert.That(scope.HeldCount, Is.EqualTo(1));
        }

        [Test]
        public void Ending_releases_everything_it_loaded_newest_first()
        {
            LoadNow("shambler");
            LoadNow("rumble");
            var scene = scope.LoadSceneAsync("Level", CancellationToken.None);
            source.Finish("Level", default(Scene));
            Result(scene);

            End();

            Assert.That(source.Released, Is.EqualTo(new[] { "Level", "rumble", "shambler" }));
            Assert.That(scope.HeldCount, Is.Zero);
            Assert.That(scope.IsEnded, Is.True);
        }

        [Test]
        public void Ending_again_releases_nothing_more()
        {
            LoadNow("shambler");

            End();
            End();

            Assert.That(source.Released, Is.EqualTo(new[] { "shambler" }));
        }

        [Test]
        public void Nothing_is_released_before_the_scope_ends()
        {
            LoadNow("shambler");
            LoadNow("shot");

            Assert.That(source.Released, Is.Empty);
        }

        [Test]
        public void Releases_a_load_still_running_when_the_scope_ends_and_cancels_its_caller()
        {
            var load = scope.LoadAssetAsync<string>("shambler", CancellationToken.None);

            End();
            source.Finish("shambler", "Shambler prefab");

            Assert.That(source.Released, Is.EqualTo(new[] { "shambler" }));
            Assert.Throws<OperationCanceledException>(() => Result(load));
        }

        [Test]
        public void Cannot_load_once_ended()
        {
            End();

            Assert.Throws<ObjectDisposedException>(() => Result(scope.LoadAssetAsync<string>("shambler", CancellationToken.None)));
            Assert.Throws<ObjectDisposedException>(() => Result(scope.LoadSceneAsync("Level", CancellationToken.None)));
            Assert.That(source.Started, Is.Empty);
        }

        [Test]
        public void Cancelling_a_load_stops_the_wait_but_the_scope_keeps_and_later_releases_it()
        {
            using var cancellation = new CancellationTokenSource();
            var load = scope.LoadAssetAsync<string>("shambler", cancellation.Token);

            cancellation.Cancel();
            source.Finish("shambler", "Shambler prefab");

            Assert.Throws<OperationCanceledException>(() => Result(load));
            Assert.That(source.Released, Is.Empty);
            Assert.That(Result(scope.LoadAssetAsync<string>("shambler", CancellationToken.None)), Is.EqualTo("Shambler prefab"));

            End();

            Assert.That(source.Released, Is.EqualTo(new[] { "shambler" }));
        }

        [Test]
        public void Does_not_start_a_load_with_a_cancelled_token()
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            Assert.Throws<OperationCanceledException>(() => Result(scope.LoadAssetAsync<string>("shambler", cancellation.Token)));
            Assert.That(source.Started, Is.Empty);
        }

        [Test]
        public void Releases_a_failed_load_once_and_forgets_it()
        {
            var first = scope.LoadAssetAsync<string>("missing", CancellationToken.None);
            var second = scope.LoadAssetAsync<string>("missing", CancellationToken.None);

            source.Fail("missing");

            Assert.Throws<InvalidOperationException>(() => Result(first));
            Assert.Throws<InvalidOperationException>(() => Result(second));
            Assert.That(source.Released, Is.EqualTo(new[] { "missing" }));
            Assert.That(scope.HeldCount, Is.Zero);
            Assert.Throws<InvalidOperationException>(() => scope.Get<string>("missing"));

            End();

            Assert.That(source.Released, Is.EqualTo(new[] { "missing" }));
        }

        [Test]
        public void Get_refuses_an_asset_that_has_not_arrived()
        {
            scope.LoadAssetAsync<string>("shambler", CancellationToken.None).Forget();

            Assert.Throws<InvalidOperationException>(() => scope.Get<string>("shambler"));
            Assert.Throws<InvalidOperationException>(() => scope.Get<string>("never asked for"));
        }

        [Test]
        public void Each_scene_load_is_its_own_scene_to_unload()
        {
            var first = scope.LoadSceneAsync("Level", CancellationToken.None);
            var second = scope.LoadSceneAsync("Level", CancellationToken.None);
            source.FinishAll("Level", default(Scene));
            Result(first);
            Result(second);

            End();

            Assert.That(source.Started, Is.EqualTo(new[] { "Level", "Level" }));
            Assert.That(source.Released, Is.EqualTo(new[] { "Level", "Level" }));
        }

        [Test]
        public void Loads_a_list_of_assets_at_once()
        {
            var load = scope.LoadAssetsAsync<string>(new object[] { "shot", "spark" }, CancellationToken.None);
            source.Finish("spark", "Hit spark prefab");
            source.Finish("shot", "Shot prefab");
            Result(load);

            Assert.That(scope.Get<string>("shot"), Is.EqualTo("Shot prefab"));
            Assert.That(scope.Get<string>("spark"), Is.EqualTo("Hit spark prefab"));
        }

        private void LoadNow(string key)
        {
            var load = scope.LoadAssetAsync<string>(key, CancellationToken.None);
            source.Finish(key, key);
            Result(load);
        }

        private void End()
        {
            Result(scope.EndAsync());
        }

        private static T Result<T>(UniTask<T> task)
        {
            Assert.That(task.Status, Is.Not.EqualTo(UniTaskStatus.Pending), "The load hasn't finished.");
            return task.GetAwaiter().GetResult();
        }

        private static void Result(UniTask task)
        {
            Assert.That(task.Status, Is.Not.EqualTo(UniTaskStatus.Pending), "The work hasn't finished.");
            task.GetAwaiter().GetResult();
        }

        // Loads finish only when a test says so, and releases are recorded by key.
        private sealed class FakeSource : IContentSource
        {
            private readonly List<FakeHandle> running = new();

            public List<string> Started { get; } = new();
            public List<string> Released { get; } = new();

            public IContentHandle LoadAsset<T>(object key) where T : class
            {
                return Start(key);
            }

            public IContentHandle LoadScene(object key)
            {
                return Start(key);
            }

            public void Finish(string key, object loaded)
            {
                var handle = running.Find(h => h.Key == key);
                running.Remove(handle);
                handle.Completion.TrySetResult(loaded);
            }

            public void FinishAll(string key, object loaded)
            {
                while (running.Exists(h => h.Key == key))
                {
                    Finish(key, loaded);
                }
            }

            public void Fail(string key)
            {
                var handle = running.Find(h => h.Key == key);
                running.Remove(handle);
                handle.Completion.TrySetException(new InvalidOperationException($"No asset at '{key}'."));
            }

            private FakeHandle Start(object key)
            {
                var handle = new FakeHandle((string)key, this);
                Started.Add(handle.Key);
                running.Add(handle);
                return handle;
            }

            private sealed class FakeHandle : IContentHandle
            {
                private readonly FakeSource source;

                public FakeHandle(string key, FakeSource owner)
                {
                    Key = key;
                    source = owner;
                }

                public string Key { get; }
                public UniTaskCompletionSource<object> Completion { get; } = new();
                public UniTask<object> Loaded => Completion.Task;

                public UniTask ReleaseAsync()
                {
                    source.Released.Add(Key);
                    return UniTask.CompletedTask;
                }
            }
        }
    }
}
