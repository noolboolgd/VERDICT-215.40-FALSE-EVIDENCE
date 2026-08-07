using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using System.Linq;

public class DynamicVideoController : MonoBehaviour
{
    [Header("Video Settings")]
    [Tooltip("Array of video clips to play")]
    public VideoClip[] videoClips;
    
    [Tooltip("URLs for video files (use if you want to load from file path)")]
    public string[] videoURLs;
    
    [Tooltip("Whether to use URLs or VideoClips")]
    public bool useURLs = false;
    
    [Header("Playback Settings")]
    [Tooltip("Time in seconds before switching to next video (0 = manual switch only)")]
    public float switchInterval = 10f;
    
    [Tooltip("Whether to loop a single video (overrides switching)")]
    public bool loopSingleVideo = false;
    
    [Tooltip("Index of video to loop if loopSingleVideo is true")]
    public int loopVideoIndex = 0;
    
    [Tooltip("Whether to play videos in random order")]
    public bool randomOrder = false;
    
    [Tooltip("Whether to automatically start playing")]
    public bool playOnStart = true;
    
    [Header("Video Player Settings")]
    [Tooltip("Reference to VideoPlayer component")]
    public VideoPlayer videoPlayer;
    
    [Tooltip("Reference to RawImage for display")]
    public RawImage displayImage;
    
    [Tooltip("Whether to play audio from the video")]
    public bool playAudio = true;
    
    [Tooltip("Whether to skip frames if performance issues")]
    public bool skipOnDrop = true;
    
    [Tooltip("Video render mode")]
    public VideoRenderMode renderMode = VideoRenderMode.RenderTexture;
    
    [Header("Trigger Settings")]
    [Tooltip("Trigger collider to switch videos")]
    public Collider triggerCollider;
    
    [Tooltip("Tag to detect for trigger switching")]
    public string triggerTag = "Player";
    
    [Tooltip("Switch on trigger enter")]
    public bool switchOnTriggerEnter = true;
    
    [Tooltip("Switch on trigger stay")]
    public bool switchOnTriggerStay = false;
    
    [Tooltip("Switch on trigger exit")]
    public bool switchOnTriggerExit = false;
    
    [Header("Transition Settings")]
    [Tooltip("Fade duration between videos (seconds)")]
    public float fadeDuration = 1f;
    
    [Tooltip("Whether to use crossfade between videos")]
    public bool useCrossfade = false;
    
    [Header("Display Settings")]
    [Tooltip("Aspect ratio fit mode")]
    public VideoAspectRatio aspectRatio = VideoAspectRatio.FitVertically;
    
    [Tooltip("Whether to show video title/name on screen")]
    public bool showVideoInfo = false;
    
    [Tooltip("UI Text to display video name")]
    public Text videoNameText;
    
    [Header("Status")]
    [Tooltip("Current video index")]
    public int currentVideoIndex = 0;
    
    [Tooltip("Whether a video is currently playing")]
    public bool isPlaying = false;
    
    // Private variables
    private Coroutine switchCoroutine;
    private bool isSwitching = false;
    private List<int> playOrder = new List<int>();
    private int currentPlayIndex = 0;
    
    void Start()
    {
        // Setup video player if not assigned
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();
        
        if (videoPlayer == null)
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
        
        // Setup display if not assigned
        if (displayImage == null)
            displayImage = GetComponent<RawImage>();
        
        // Setup trigger collider if not assigned
        if (triggerCollider == null)
            triggerCollider = GetComponent<Collider>();
        
        // Configure video player
        ConfigureVideoPlayer();
        
        // Initialize play order
        InitializePlayOrder();
        
        // Start playing
        if (playOnStart)
            PlayVideo(currentVideoIndex);
    }
    
    void ConfigureVideoPlayer()
    {
        // Basic video player settings
        videoPlayer.renderMode = renderMode;
        videoPlayer.aspectRatio = aspectRatio;
        videoPlayer.skipOnDrop = skipOnDrop;
        videoPlayer.audioOutputMode = playAudio ? VideoAudioOutputMode.AudioSource : VideoAudioOutputMode.None;
        
        // Enable loop for single video mode
        if (loopSingleVideo)
        {
            videoPlayer.isLooping = true;
        }
        else
        {
            videoPlayer.isLooping = false;
            videoPlayer.loopPointReached += OnVideoFinished;
        }
        
        // Setup render texture for display
        if (displayImage != null)
        {
            RenderTexture renderTexture = new RenderTexture(1920, 1080, 0);
            videoPlayer.targetTexture = renderTexture;
            displayImage.texture = renderTexture;
        }
    }
    
    void InitializePlayOrder()
    {
        playOrder.Clear();
        
        int videoCount = useURLs ? videoURLs.Length : videoClips.Length;
        
        if (randomOrder)
        {
            // Create random order
            for (int i = 0; i < videoCount; i++)
                playOrder.Add(i);
            
            // Shuffle
            for (int i = 0; i < playOrder.Count; i++)
            {
                int temp = playOrder[i];
                int randomIndex = Random.Range(i, playOrder.Count);
                playOrder[i] = playOrder[randomIndex];
                playOrder[randomIndex] = temp;
            }
        }
        else
        {
            // Sequential order
            for (int i = 0; i < videoCount; i++)
                playOrder.Add(i);
        }
    }
    
    void Update()
    {
        // Handle trigger switching if collider exists
        if (triggerCollider != null && !isSwitching)
        {
            // Check if trigger collider is enabled
            if (triggerCollider.enabled)
            {
                // We handle trigger events through OnTrigger methods
            }
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (switchOnTriggerEnter && IsValidTrigger(other))
        {
            SwitchToNextVideo();
        }
    }
    
    void OnTriggerStay(Collider other)
    {
        if (switchOnTriggerStay && IsValidTrigger(other))
        {
            SwitchToNextVideo();
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (switchOnTriggerExit && IsValidTrigger(other))
        {
            SwitchToNextVideo();
        }
    }
    
    bool IsValidTrigger(Collider other)
    {
        if (string.IsNullOrEmpty(triggerTag))
            return true;
        
        return other.CompareTag(triggerTag);
    }
    
    public void PlayVideo(int index)
    {
        if (isSwitching)
            return;
        
        currentVideoIndex = index;
        isPlaying = true;
        
        // Load video based on source type
        if (useURLs && index < videoURLs.Length && !string.IsNullOrEmpty(videoURLs[index]))
        {
            videoPlayer.url = videoURLs[index];
        }
        else if (!useURLs && index < videoClips.Length && videoClips[index] != null)
        {
            videoPlayer.clip = videoClips[index];
        }
        else
        {
            Debug.LogWarning("Video at index " + index + " is null or invalid!");
            return;
        }
        
        // Update video info display
        if (showVideoInfo && videoNameText != null)
        {
            string videoName = GetVideoName(index);
            videoNameText.text = videoName;
        }
        
        // Play the video
        videoPlayer.Play();
        
        // Start auto-switch coroutine if interval > 0 and not looping single video
        if (switchInterval > 0 && !loopSingleVideo)
        {
            if (switchCoroutine != null)
                StopCoroutine(switchCoroutine);
            switchCoroutine = StartCoroutine(AutoSwitchCoroutine());
        }
        
        Debug.Log("Playing video " + index + ": " + GetVideoName(index));
    }
    
    string GetVideoName(int index)
    {
        if (useURLs && index < videoURLs.Length)
            return System.IO.Path.GetFileName(videoURLs[index]);
        else if (!useURLs && index < videoClips.Length && videoClips[index] != null)
            return videoClips[index].name;
        else
            return "Video " + index;
    }
    
    IEnumerator AutoSwitchCoroutine()
    {
        float elapsedTime = 0;
        
        while (elapsedTime < switchInterval)
        {
            elapsedTime += Time.deltaTime;
            
            // Check if video ended early
            if (!videoPlayer.isPlaying && !loopSingleVideo)
            {
                break;
            }
            
            yield return null;
        }
        
        // Switch to next video if still playing
        if (videoPlayer.isPlaying || !loopSingleVideo)
        {
            SwitchToNextVideo();
        }
    }
    
    public void SwitchToNextVideo()
    {
        if (isSwitching || loopSingleVideo)
            return;
        
        StartCoroutine(SwitchVideoCoroutine());
    }
    
    IEnumerator SwitchVideoCoroutine()
    {
        isSwitching = true;
        
        // Get next video index
        int nextIndex = GetNextVideoIndex();
        
        // Fade out if crossfade is enabled
        if (useCrossfade && displayImage != null)
        {
            yield return StartCoroutine(FadeVideo(0f, fadeDuration));
        }
        
        // Stop current video
        videoPlayer.Stop();
        
        // Play next video
        PlayVideo(nextIndex);
        
        // Fade in if crossfade is enabled
        if (useCrossfade && displayImage != null)
        {
            yield return StartCoroutine(FadeVideo(1f, fadeDuration));
        }
        
        isSwitching = false;
    }
    
    IEnumerator FadeVideo(float targetAlpha, float duration)
    {
        if (displayImage == null)
            yield break;
        
        Color startColor = displayImage.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, targetAlpha);
        float elapsedTime = 0;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            displayImage.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }
        
        displayImage.color = targetColor;
    }
    
    int GetNextVideoIndex()
    {
        int videoCount = useURLs ? videoURLs.Length : videoClips.Length;
        
        if (videoCount == 0)
            return 0;
        
        if (loopSingleVideo)
            return loopVideoIndex % videoCount;
        
        // Get next index from play order
        currentPlayIndex = (currentPlayIndex + 1) % playOrder.Count;
        return playOrder[currentPlayIndex];
    }
    
    void OnVideoFinished(VideoPlayer vp)
    {
        if (!loopSingleVideo && !isSwitching)
        {
            SwitchToNextVideo();
        }
    }
    
    // Public methods for external control
    
    public void PlayNextVideo()
    {
        SwitchToNextVideo();
    }
    
    public void PlayPreviousVideo()
    {
        if (isSwitching || loopSingleVideo)
            return;
        
        int videoCount = useURLs ? videoURLs.Length : videoClips.Length;
        currentPlayIndex = (currentPlayIndex - 1 + playOrder.Count) % playOrder.Count;
        int previousIndex = playOrder[currentPlayIndex];
        
        PlayVideo(previousIndex);
    }
    
    public void PauseVideo()
    {
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
            isPlaying = false;
        }
    }
    
    public void ResumeVideo()
    {
        if (!videoPlayer.isPlaying)
        {
            videoPlayer.Play();
            isPlaying = true;
        }
    }
    
    public void StopVideo()
    {
        videoPlayer.Stop();
        isPlaying = false;
    }
    
    public void SetVolume(float volume)
    {
        videoPlayer.SetDirectAudioVolume(0, Mathf.Clamp01(volume));
    }
    
    public void SetPlaybackSpeed(float speed)
    {
        videoPlayer.playbackSpeed = Mathf.Max(0.1f, speed);
    }
    
    public void SetSwitchInterval(float interval)
    {
        switchInterval = Mathf.Max(0, interval);
        
        // Restart coroutine if playing
        if (isPlaying && switchInterval > 0 && !loopSingleVideo)
        {
            if (switchCoroutine != null)
                StopCoroutine(switchCoroutine);
            switchCoroutine = StartCoroutine(AutoSwitchCoroutine());
        }
    }
    
    // Editor-only methods for convenience
    void OnValidate()
    {
        // Ensure loop video index is valid
        int videoCount = useURLs ? videoURLs.Length : videoClips.Length;
        if (loopVideoIndex >= videoCount)
            loopVideoIndex = Mathf.Max(0, videoCount - 1);
        
        // Setup video player if not assigned
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();
    }
}
