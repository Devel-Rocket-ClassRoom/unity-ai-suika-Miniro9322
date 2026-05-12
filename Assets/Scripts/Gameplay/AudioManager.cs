using UnityEngine;
using SuikaGame.Gameplay;
using SuikaGame.Data;

namespace SuikaGame.Audio
{
    /// <summary>
    /// BGM 과 SFX 재생을 총괄하는 싱글톤 매니저.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("오디오 소스")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("오디오 클립")]
        public AudioClip bgmClip;
        public AudioClip dropClip;
        public AudioClip mergeClip;
        public AudioClip gameOverClip;
        public AudioClip buttonClickClip;

        private void Awake()
        {
            Instance = this;

            if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();

            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            DropSpawner.OnDroppedStatic += HandleDropped;
            MergeSpawner.OnMergeStatic += HandleMerge;
            GameStateManager.OnGameOverStatic += HandleGameOver;
        }

        private void OnDisable()
        {
            DropSpawner.OnDroppedStatic -= HandleDropped;
            MergeSpawner.OnMergeStatic -= HandleMerge;
            GameStateManager.OnGameOverStatic -= HandleGameOver;
        }

        private void Start()
        {
            PlayBGM();
        }

        public void PlayBGM()
        {
            if (bgmClip != null && bgmSource != null)
            {
                bgmSource.clip = bgmClip;
                bgmSource.Play();
            }
        }

        public void PlaySFX(AudioClip clip)
        {
            if (clip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }

        private void HandleDropped(FruitData data, Fruit fruit)
        {
            PlaySFX(dropClip);
        }

        private void HandleMerge(int level, Vector2 pos, int score)
        {
            PlaySFX(mergeClip);
        }

        private void HandleGameOver()
        {
            if (bgmSource != null) bgmSource.Stop();
            PlaySFX(gameOverClip);
        }

        public void PlayButtonClick()
        {
            PlaySFX(buttonClickClip);
        }
    }
}
