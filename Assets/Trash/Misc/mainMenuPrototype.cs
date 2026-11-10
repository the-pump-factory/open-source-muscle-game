using Swole;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class mainMenuPrototype : MonoBehaviour
{

    public static bool playedIntro;

    public AudioSource introSong;

    [Serializable]
    public class ParallaxObject
    {
        public RectTransform rT;
        public float parallaxAmount;

        [NonSerialized]
        public Vector3 startPosition;
    }

    public float introOffsetX = -400;
    public float introOffsetY = -200;
    protected float introTime;

    public float introCenteringTime = 4;

    public float cursorCenterOffsetMaxDistanceX = 800;
    public float parallaxOffsetDistanceX = 100;

    public float cursorCenterOffsetMaxDistanceY = 500;
    public float parallaxOffsetDistanceY = 30;

    public ParallaxObject[] parallaxObjects;

    void Awake()
    {
        if (!playedIntro)
        {
            playedIntro = true;
            if (introSong != null) introSong.Play();

            introTime = introCenteringTime;
        } 

        if (parallaxObjects != null)
        {
            foreach(var obj in parallaxObjects)
            {
                if (obj == null || obj.rT == null) continue;

                obj.startPosition = obj.rT.localPosition;
            }
        }
    }

    void Update()
    {
        if (parallaxObjects != null)
        {

            introTime -= Time.deltaTime;

            var cursorPos = CursorProxy.ScreenPosition;

            var screenSizeX = Screen.width;
            var screenCenterX = (screenSizeX / 2f) + Mathf.SmoothStep(0, introOffsetX, introTime / introCenteringTime);

            var screenSizeY = Screen.height;
            var screenCenterY = (screenSizeY / 2f) + Mathf.SmoothStep(0, introOffsetY, introTime / introCenteringTime);

            float offsetX = (cursorPos.x - screenCenterX) / cursorCenterOffsetMaxDistanceX;
            float offsetY = (cursorPos.y - screenCenterY) / cursorCenterOffsetMaxDistanceY; 

            foreach (var obj in parallaxObjects)
            {
                if (obj == null || obj.rT == null) continue;

                obj.rT.localPosition = obj.startPosition - new Vector3(offsetX * parallaxOffsetDistanceX * obj.parallaxAmount, offsetY * parallaxOffsetDistanceY * obj.parallaxAmount, 0); 
            }
        }
    }

    public void Quit()
    {
        Application.Quit();
    }

}
