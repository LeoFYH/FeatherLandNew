using System;
using UnityEngine;
using UnityEngine.Video;

namespace HiyodoGame
{
    public class VideoPrepare : MonoBehaviour
    {
        private VideoPlayer _videoPlayer;

        private void Awake()
        {
            _videoPlayer = GetComponent<VideoPlayer>();
            _videoPlayer.Prepare();
        }
    }
}
