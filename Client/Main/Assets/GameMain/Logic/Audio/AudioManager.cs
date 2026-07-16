using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Dragon.Resource;
using UnityEngine;

namespace Dragon.Audio
{
    /// <summary>
    /// 音频管理：BGM / 环境音双通道交叉淡化，音效多通道池；资源通过 ResourceManager（Addressables 地址）加载。
    /// </summary>
    public class AudioManager
    {
        #region Singleton

        protected static AudioManager _instance;

        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AudioManager();
                }
                return _instance;
            }
        }

        #endregion

        private const int DefaultSfxChannelCount = 8;

        private readonly object _sfxLock = new object();

        private const int CrossFadeSourceCount = 2;

        private GameObject _root;
        private SfxChannel[] _sfxChannels;

        private MusicChannelGroup _bgmGroup;
        private MusicChannelGroup _ambientGroup;

        private float _bgmVolume = 1f;
        private float _ambientVolume = 1f;
        private float _sfxVolume = 1f;
        private float _defaultMusicFadeInDuration = 1f;
        private float _defaultMusicFadeOutDuration = 1f;

        private sealed class SfxChannel
        {
            public AudioSource Source;
            public int OpId;
            public CancellationTokenSource Cts;
            public bool Busy;
        }

        private sealed class MusicChannelGroup
        {
            public string Name;
            public float MasterVolume;
            public AudioSource[] Sources;
            public string[] Addresses;
            public float[] RelativeVolumes;
            public bool[] PreservePlaybackPositions;
            public Dictionary<string, float> PlaybackPositions;
            public int ActiveIndex = -1;
            public int OpId;
            public CancellationTokenSource FadeCts;
        }

        public float BgmVolume
        {
            get => _bgmVolume;
            set
            {
                _bgmVolume = Mathf.Clamp01(value);
                if (_bgmGroup != null)
                {
                    _bgmGroup.MasterVolume = _bgmVolume;
                }
                ApplyMasterVolume(_bgmGroup);
            }
        }

        public float AmbientVolume
        {
            get => _ambientVolume;
            set
            {
                _ambientVolume = Mathf.Clamp01(value);
                if (_ambientGroup != null)
                {
                    _ambientGroup.MasterVolume = _ambientVolume;
                }
                ApplyMasterVolume(_ambientGroup);
            }
        }

        public float SfxVolume
        {
            get => _sfxVolume;
            set => _sfxVolume = Mathf.Clamp01(value);
        }

        /// <summary>BGM/环境音默认淡入时长（秒）。</summary>
        public float DefaultMusicFadeInDuration
        {
            get => _defaultMusicFadeInDuration;
            set => _defaultMusicFadeInDuration = Mathf.Max(0f, value);
        }

        /// <summary>BGM/环境音默认淡出时长（秒）。</summary>
        public float DefaultMusicFadeOutDuration
        {
            get => _defaultMusicFadeOutDuration;
            set => _defaultMusicFadeOutDuration = Mathf.Max(0f, value);
        }

        /// <param name="sfxChannelCount">音效同时播放上限；全部占用时会抢占第 0 路。</param>
        public void Init(int sfxChannelCount = DefaultSfxChannelCount)
        {
            Shutdown();

            sfxChannelCount = Mathf.Max(1, sfxChannelCount);
            _root = new GameObject("[AudioManager]");
            UnityEngine.Object.DontDestroyOnLoad(_root);

            _bgmGroup = CreateMusicGroup("BGM", _bgmVolume);
            _ambientGroup = CreateMusicGroup("Ambient", _ambientVolume);

            var sfxRoot = new GameObject("SFX");
            sfxRoot.transform.SetParent(_root.transform, false);
            _sfxChannels = new SfxChannel[sfxChannelCount];
            for (var i = 0; i < sfxChannelCount; i++)
            {
                var go = new GameObject($"SFX_{i}");
                go.transform.SetParent(sfxRoot.transform, false);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                _sfxChannels[i] = new SfxChannel { Source = src, OpId = 0, Cts = null, Busy = false };
            }
        }

        public void Shutdown()
        {
            StopAndReleaseAllMusicSources(_bgmGroup);
            StopAndReleaseAllMusicSources(_ambientGroup);
            _bgmGroup = null;
            _ambientGroup = null;

            lock (_sfxLock)
            {
                if (_sfxChannels != null)
                {
                    foreach (var ch in _sfxChannels)
                    {
                        ch.Cts?.Cancel();
                        ch.Cts?.Dispose();
                        ch.Cts = null;
                        if (ch.Source != null)
                        {
                            ch.Source.Stop();
                            ch.Source.clip = null;
                        }
                        ch.Busy = false;
                    }
                    _sfxChannels = null;
                }
            }

            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }
        }

        public void StopBgm()
        {
            StopBgm(DefaultMusicFadeOutDuration);
        }

        public void StopBgm(float fadeOutDuration)
        {
            StopMusicInternalAsync(_bgmGroup, fadeOutDuration, CancellationToken.None).Forget();
        }

        public void PlayBgm(string address, bool loop = true, float volume = 1f, float? fadeInDuration = null,
            float? fadeOutDuration = null, bool preservePlaybackPosition = false)
        {
            PlayBgmAsync(address, loop, volume, fadeInDuration, fadeOutDuration, CancellationToken.None,
                preservePlaybackPosition).Forget();
        }

        public async UniTask PlayBgmAsync(string address, bool loop = true, float volume = 1f, float? fadeInDuration = null,
            float? fadeOutDuration = null,
            CancellationToken cancellationToken = default,
            bool preservePlaybackPosition = false)
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogWarning("[AudioManager] BGM 地址为空");
                return;
            }

            if (_bgmGroup == null || _bgmGroup.Sources == null)
            {
                Debug.LogWarning("[AudioManager] 未初始化，请先调用 Init()");
                return;
            }
            await PlayMusicAsync(_bgmGroup, address, loop, volume, fadeInDuration, fadeOutDuration, cancellationToken,
                preservePlaybackPosition);
        }

        public void StopAmbient()
        {
            StopAmbient(DefaultMusicFadeOutDuration);
        }

        public void StopAmbient(float fadeOutDuration)
        {
            StopMusicInternalAsync(_ambientGroup, fadeOutDuration, CancellationToken.None).Forget();
        }

        public void PlayAmbient(string address, bool loop = true, float volume = 1f, float? fadeInDuration = null,
            float? fadeOutDuration = null)
        {
            PlayAmbientAsync(address, loop, volume, fadeInDuration, fadeOutDuration, CancellationToken.None).Forget();
        }

        public async UniTask PlayAmbientAsync(string address, bool loop = true, float volume = 1f,
            float? fadeInDuration = null, float? fadeOutDuration = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogWarning("[AudioManager] 环境音地址为空");
                return;
            }

            if (_ambientGroup == null || _ambientGroup.Sources == null)
            {
                Debug.LogWarning("[AudioManager] 未初始化，请先调用 Init()");
                return;
            }
            await PlayMusicAsync(_ambientGroup, address, loop, volume, fadeInDuration, fadeOutDuration, cancellationToken,
                preservePlaybackPosition: false);
        }

        public void PlaySfx(string address, float volume = 1f)
        {
            PlaySfxAsync(address, volume, CancellationToken.None).Forget();
        }

        /// <summary>音效播放完成（或被取消/抢占）后完成。</summary>
        public UniTask PlaySfxAsync(string address, float volume = 1f,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(address))
            {
                // Debug.LogWarning("[AudioManager] 音效地址为空");
                return UniTask.CompletedTask;
            }

            var ch = AcquireSfxChannel();
            var opId = ch.OpId;
            return SfxPlayRoutine(ch, opId, address, volume, cancellationToken);
        }

        private SfxChannel AcquireSfxChannel()
        {
            lock (_sfxLock)
            {
                if (_sfxChannels == null || _sfxChannels.Length == 0)
                {
                    throw new InvalidOperationException("[AudioManager] 未初始化，请先调用 Init()");
                }

                for (var i = 0; i < _sfxChannels.Length; i++)
                {
                    var ch = _sfxChannels[i];
                    if (!ch.Busy)
                    {
                        PrepareChannel(ch);
                        return ch;
                    }
                }

                var victim = _sfxChannels[0];
                victim.Cts?.Cancel();
                victim.Cts?.Dispose();
                victim.Source.Stop();
                victim.Source.clip = null;
                PrepareChannel(victim);
                return victim;
            }
        }

        private static void PrepareChannel(SfxChannel ch)
        {
            ch.Busy = true;
            ch.OpId++;
            ch.Cts?.Cancel();
            ch.Cts?.Dispose();
            ch.Cts = new CancellationTokenSource();
        }

        private async UniTask SfxPlayRoutine(SfxChannel ch, int opId, string address, float volume,
            CancellationToken externalCt)
        {
            var loaded = false;
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ch.Cts.Token, externalCt);
            var ct = linked.Token;

            try
            {
                var clip = await ResourceManager.Instance.LoadAssetAsync<AudioClip>(address, ct);
                loaded = true;

                if (ch.OpId != opId || ct.IsCancellationRequested)
                {
                    return;
                }

                ch.Source.clip = clip;
                ch.Source.volume = Mathf.Clamp01(volume) * _sfxVolume;
                ch.Source.Play();

                await UniTask.WaitUntil(() => !ch.Source.isPlaying, PlayerLoopTiming.Update, ct);
            }
            catch (OperationCanceledException)
            {
                // 正常取消或抢占
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AudioManager] 音效加载失败: {address}, {ex.Message}");
            }
            finally
            {
                if (ch.OpId == opId)
                {
                    if (ch.Source != null)
                    {
                        ch.Source.Stop();
                        ch.Source.clip = null;
                    }

                    ch.Busy = false;
                }

                if (loaded)
                {
                    ResourceManager.Instance.Release<AudioClip>(address);
                }
            }
        }

        private async UniTask PlayMusicAsync(MusicChannelGroup group, string address, bool loop, float volume,
            float? fadeInDuration, float? fadeOutDuration, CancellationToken cancellationToken,
            bool preservePlaybackPosition)
        {
            if (group == null || group.Sources == null)
            {
                return;
            }

            if (group.ActiveIndex >= 0 &&
                string.Equals(group.Addresses[group.ActiveIndex], address, StringComparison.Ordinal) &&
                group.Sources[group.ActiveIndex] != null &&
                group.Sources[group.ActiveIndex].isPlaying)
            {
                group.PreservePlaybackPositions[group.ActiveIndex] = preservePlaybackPosition;
                if (!preservePlaybackPosition)
                {
                    group.PlaybackPositions.Remove(address);
                }
                return;
            }

            var opId = ++group.OpId;
            var resolvedFadeInDuration = Mathf.Max(0f, fadeInDuration ?? DefaultMusicFadeInDuration);
            var resolvedFadeOutDuration = Mathf.Max(0f, fadeOutDuration ?? DefaultMusicFadeOutDuration);
            var nextIndex = group.ActiveIndex == 0 ? 1 : 0;

            var loaded = false;
            try
            {
                var clip = await ResourceManager.Instance.LoadAssetAsync<AudioClip>(address, cancellationToken);
                loaded = true;

                if (opId != group.OpId)
                {
                    return;
                }

                var oldIndex = group.ActiveIndex;
                var targetSource = group.Sources[nextIndex];
                SavePlaybackPosition(group, nextIndex);
                ReleaseClipAt(group, nextIndex);
                targetSource.Stop();
                targetSource.clip = clip;
                targetSource.loop = loop;
                group.Addresses[nextIndex] = address;
                loaded = false; // 所有权交给通道，由 ReleaseClipAt 释放
                group.PreservePlaybackPositions[nextIndex] = preservePlaybackPosition;
                var targetRelativeVolume = Mathf.Clamp01(volume);
                group.RelativeVolumes[nextIndex] = targetRelativeVolume;
                targetSource.volume = 0f;
                ApplySavedPlaybackPosition(group, targetSource, address, preservePlaybackPosition);
                targetSource.Play();

                CancelActiveFade(group);
                group.FadeCts = new CancellationTokenSource();
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(group.FadeCts.Token, cancellationToken);
                var ct = linked.Token;
                var targetVolume = targetRelativeVolume * group.MasterVolume;

                if (oldIndex >= 0 && group.Sources[oldIndex] != null && group.Sources[oldIndex].isPlaying)
                {
                    var fadeInTask = FadeSourceVolumeAsync(group.Sources[nextIndex], targetVolume, resolvedFadeInDuration, ct);
                    var fadeOutTask = FadeSourceVolumeAsync(group.Sources[oldIndex], 0f, resolvedFadeOutDuration, ct);
                    await UniTask.WhenAll(fadeInTask, fadeOutTask);
                    if (opId != group.OpId)
                    {
                        return;
                    }

                    SavePlaybackPosition(group, oldIndex);
                    group.Sources[oldIndex].Stop();
                    group.Sources[oldIndex].clip = null;
                    group.RelativeVolumes[oldIndex] = 0f;
                    group.PreservePlaybackPositions[oldIndex] = false;
                    ReleaseClipAt(group, oldIndex);
                }
                else
                {
                    await FadeSourceVolumeAsync(group.Sources[nextIndex], targetVolume, resolvedFadeInDuration, ct);
                    if (opId != group.OpId)
                    {
                        return;
                    }
                }

                group.ActiveIndex = nextIndex;
            }
            catch (OperationCanceledException)
            {
                // 取消时若尚未交给通道，则在 finally 释放
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AudioManager] {group.Name} 异常: {address}, {ex}");
            }
            finally
            {
                if (loaded)
                {
                    ResourceManager.Instance.Release<AudioClip>(address);
                }
            }
        }

        private static async UniTask FadeSourceVolumeAsync(AudioSource source, float targetVolume, float duration,
            CancellationToken externalCt)
        {
            if (source == null)
            {
                return;
            }

            DOTween.Kill(source);

            var resolvedVolume = Mathf.Max(0f, targetVolume);
            if (duration <= 0f)
            {
                source.volume = resolvedVolume;
                return;
            }

            try
            {
                var tween = source
                    .DOFade(resolvedVolume, Mathf.Max(0.01f, duration))
                    .SetUpdate(true);
                await tween.AsyncWaitForCompletion()
                    .AsUniTask()
                    .AttachExternalCancellation(externalCt);
                source.volume = resolvedVolume;
            }
            catch (OperationCanceledException)
            {
                // 淡化被新请求打断属于正常情况
            }
        }

        private static void ApplySavedPlaybackPosition(MusicChannelGroup group, AudioSource source, string address,
            bool preservePlaybackPosition)
        {
            if (!preservePlaybackPosition)
            {
                group.PlaybackPositions.Remove(address);
                return;
            }

            if (source?.clip == null ||
                !group.PlaybackPositions.TryGetValue(address, out float playbackPosition) ||
                source.clip.length <= 0f)
            {
                return;
            }

            source.time = Mathf.Repeat(playbackPosition, source.clip.length);
        }

        private static void SavePlaybackPosition(MusicChannelGroup group, int index)
        {
            if (group?.Sources == null ||
                index < 0 ||
                index >= group.Sources.Length ||
                !group.PreservePlaybackPositions[index] ||
                string.IsNullOrEmpty(group.Addresses[index]))
            {
                return;
            }

            AudioSource source = group.Sources[index];
            if (source?.clip == null)
            {
                return;
            }

            group.PlaybackPositions[group.Addresses[index]] = source.time;
        }

        private async UniTask StopMusicInternalAsync(MusicChannelGroup group, float fadeOutDuration, CancellationToken cancellationToken)
        {
            if (group == null || group.Sources == null || group.ActiveIndex < 0)
            {
                return;
            }

            group.OpId++;
            var index = group.ActiveIndex;
            var source = group.Sources[index];

            CancelActiveFade(group);
            group.FadeCts = new CancellationTokenSource();
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(group.FadeCts.Token, cancellationToken);
            var ct = linked.Token;

            if (source != null && source.isPlaying)
            {
                await FadeSourceVolumeAsync(source, 0f, Mathf.Max(0f, fadeOutDuration), ct);
                SavePlaybackPosition(group, index);
                source.Stop();
                source.clip = null;
            }

            group.RelativeVolumes[index] = 0f;
            group.PreservePlaybackPositions[index] = false;
            ReleaseClipAt(group, index);
            group.ActiveIndex = -1;
        }

        private MusicChannelGroup CreateMusicGroup(string groupName, float initialVolume)
        {
            var groupGo = new GameObject(groupName);
            groupGo.transform.SetParent(_root.transform, false);
            var sources = new AudioSource[CrossFadeSourceCount];

            for (var i = 0; i < CrossFadeSourceCount; i++)
            {
                var go = new GameObject($"{groupName}_{i}");
                go.transform.SetParent(groupGo.transform, false);
                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = true;
                source.volume = 0f;
                sources[i] = source;
            }

            if (sources.Length > 0)
            {
                sources[0].volume = initialVolume;
            }

            return new MusicChannelGroup
            {
                Name = groupName,
                MasterVolume = Mathf.Clamp01(initialVolume),
                Sources = sources,
                Addresses = new string[CrossFadeSourceCount],
                RelativeVolumes = new float[CrossFadeSourceCount],
                PreservePlaybackPositions = new bool[CrossFadeSourceCount],
                PlaybackPositions = new Dictionary<string, float>(),
                ActiveIndex = -1,
                OpId = 0,
                FadeCts = null
            };
        }

        private void StopAndReleaseAllMusicSources(MusicChannelGroup group)
        {
            if (group == null || group.Sources == null)
            {
                return;
            }

            group.OpId++;
            group.ActiveIndex = -1;
            CancelActiveFade(group);

            for (var i = 0; i < group.Sources.Length; i++)
            {
                if (group.Sources[i] != null)
                {
                    group.Sources[i].Stop();
                    group.Sources[i].clip = null;
                }
                group.RelativeVolumes[i] = 0f;
                group.PreservePlaybackPositions[i] = false;
                ReleaseClipAt(group, i);
            }

            group.PlaybackPositions.Clear();
        }

        private static void ReleaseClipAt(MusicChannelGroup group, int index)
        {
            if (group?.Addresses == null || index < 0 || index >= group.Addresses.Length)
            {
                return;
            }

            var address = group.Addresses[index];
            group.Addresses[index] = null;
            if (!string.IsNullOrEmpty(address))
            {
                ResourceManager.Instance.Release<AudioClip>(address);
            }
        }

        private static void CancelActiveFade(MusicChannelGroup group)
        {
            KillFadeTweensOnGroup(group);
            CancelAndDisposeFade(ref group.FadeCts);
        }

        private static void KillFadeTweensOnGroup(MusicChannelGroup group)
        {
            if (group?.Sources == null)
            {
                return;
            }

            for (var i = 0; i < group.Sources.Length; i++)
            {
                if (group.Sources[i] != null)
                {
                    DOTween.Kill(group.Sources[i]);
                }
            }
        }

        private static void CancelAndDisposeFade(ref CancellationTokenSource cts)
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }

        private static void ApplyMasterVolume(MusicChannelGroup group)
        {
            if (group == null || group.Sources == null)
            {
                return;
            }

            for (var i = 0; i < group.Sources.Length; i++)
            {
                if (group.Sources[i] != null)
                {
                    group.Sources[i].volume = group.RelativeVolumes[i] * group.MasterVolume;
                }
            }
        }
    }
}