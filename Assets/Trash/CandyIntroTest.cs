using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Swole;
using Swole.API.Unity;
using Swole.API.Unity.Animation;
using System;
using UnityEngine.Audio;
using Swole.Animation;

public class CandyIntroTest : MonoBehaviour
{

    public bool play;
    public bool stop;

    public CustomAnimator characterAnimator;
    public CustomAnimator cameraAnimator;

    [Serializable]
    public class Anim
    {
        public string name;
        public string package = "candy.intro";
        [NonSerialized]
        protected CustomAnimation asset;
        public CustomAnimation Asset
        {
            get
            {
                if (asset == null) asset = Swole.ContentManager.FindContent<CustomAnimation>(name, new Swole.PackageIdentifier(package));
                return asset;
            }
        }
        public float Length
        {
            get
            {
                return Asset.LengthInSeconds;
            }
        }
        [NonSerialized]
        public IAnimationLayer layer;

        public float speed = 0;
        public bool loop = false;
        public float startTimeNormalized;

        public float fadeIn = 0;
        public float fadeOut = 1;

        public int[] nullifyingDependencies;
        public float GetMix(Anim[] anims)
        {
            if (nullifyingDependencies == null) return 1;

            float nullMix = 0;
            foreach (var nullifier in nullifyingDependencies) nullMix = Mathf.Max(anims[nullifier].layer.Mix, nullMix);

            return Mathf.Max(0, 1 - nullMix);
        }
    }

    [Serializable]
    public class Sound
    {
        public AudioClip clip;
        public float volume = 1;
        public float pitch = 1;
        public AudioMixerGroup mixerGroup;
        public float Length
        {
            get
            {
                return clip.length;
            }
        }
        [NonSerialized]
        public PersistentAudioPlayer.AudioSourceClaim claim;
        public void Play(float startTime)
        {
            if (claim == null || !claim.IsValid) claim = PersistentAudioPlayer.Get2DSourceAndPlay(clip, volume, pitch, mixerGroup);
            claim.Source.time = startTime;
            claim.Source.Play();
        }
    }

    [Serializable]
    public class AnimTask
    {
        public string name;
        public int[] animsToPlay;
        public int[] soundsToPlay;
        public float startTime;
    }

    public AnimTask[] characterTasks;
    public Anim[] characterAnims;
    public Sound[] characterSounds;

    public AnimTask[] cameraTasks;
    public Anim[] cameraAnims;

    protected float time;
    protected float maxTime;

    public float muscleMass;
    public float breastSize;

    protected CustomAnimationController controllerCharacter;
    protected MuscularRenderedCharacter muscleCharacter;
    protected CustomAnimationController controllerCamera;
    protected CameraProxy cameraProxy;
    public void Stop()
    {
        playing = false;
        time = 0;
        foreach (var sound in characterSounds) if (sound.claim != null && sound.claim.IsValid) sound.claim.Release();
    }
    public void Restart()
    {
        Stop();

        muscleCharacter = characterAnimator.GetComponent<MuscularRenderedCharacter>(); 
        muscleCharacter.SetGlobalMass(muscleMass);
        muscleCharacter.BreastPresence = breastSize;

        if (controllerCharacter == null)
        {
            controllerCharacter = ScriptableObject.CreateInstance<CustomAnimationController>();
            controllerCharacter.name = "main";

            var animations = new AnimationReference[characterAnims.Length];
            var layers = new CustomAnimationLayer[characterAnims.Length];
            for (int a = 0; a < characterAnims.Length; a++) 
            {
                var anim = characterAnims[a];
                animations[a] = new AnimationReference(anim.name, anim.Asset, anim.loop ? AnimationLoopMode.Loop : AnimationLoopMode.PlayOnce);

                CustomAnimationLayer previewLayer = new CustomAnimationLayer();
                layers[a] = previewLayer;

                previewLayer.SetActive(true);
                previewLayer.name = anim.name;
                previewLayer.blendParameterIndex = -1;
                previewLayer.entryStateIndex = 0;
                previewLayer.mix = 1;
                previewLayer.IsAdditive = true;
                previewLayer.motionControllerIdentifiers = new MotionControllerIdentifier[] { new MotionControllerIdentifier() { index = a, type = MotionControllerType.AnimationReference } };

                CustomAnimationLayerState previewStateMachine = new CustomAnimationLayerState() { name = anim.name, motionControllerIndex = 0 };
                previewLayer.states = new CustomAnimationLayerState[] { previewStateMachine };
            }

            controllerCharacter.animationReferences = animations;
            controllerCharacter.layers = layers;
        }
        characterAnimator.RemoveControllerData(controllerCharacter);
        characterAnimator.ApplyController(controllerCharacter);
        foreach(var anim in characterAnims)
        {
            anim.layer = characterAnimator.FindLayer(controllerCharacter.Prefix + anim.name); 
            anim.layer.Mix = 0;
            anim.layer.ActiveState.SetNormalizedTime(anim.startTimeNormalized);
            var mc = anim.layer.GetMotionControllerUnsafe(anim.layer.ActiveState.MotionControllerIndex);
            mc.BaseSpeed = 0;
        }
        foreach (var sound in characterSounds) if (sound.claim != null && sound.claim.IsValid) sound.claim.Release();

        if (controllerCamera == null)
        {
            controllerCamera = ScriptableObject.CreateInstance<CustomAnimationController>();
            controllerCamera.name = "main";

            var animations = new AnimationReference[cameraAnims.Length];
            var layers = new CustomAnimationLayer[cameraAnims.Length];
            for (int a = 0; a < cameraAnims.Length; a++)
            {
                var anim = cameraAnims[a];
                animations[a] = new AnimationReference(anim.name, anim.Asset, anim.loop ? AnimationLoopMode.Loop : AnimationLoopMode.PlayOnce);

                CustomAnimationLayer previewLayer = new CustomAnimationLayer();
                layers[a] = previewLayer;

                previewLayer.SetActive(true);
                previewLayer.name = anim.name;
                previewLayer.blendParameterIndex = -1;
                previewLayer.entryStateIndex = 0;
                previewLayer.mix = 1;
                previewLayer.IsAdditive = true;
                previewLayer.motionControllerIdentifiers = new MotionControllerIdentifier[] { new MotionControllerIdentifier() { index = a, type = MotionControllerType.AnimationReference } };

                CustomAnimationLayerState previewStateMachine = new CustomAnimationLayerState() { name = anim.name, motionControllerIndex = 0 };
                previewLayer.states = new CustomAnimationLayerState[] { previewStateMachine };
            }

            controllerCamera.animationReferences = animations;
            controllerCamera.layers = layers;
        }
        cameraAnimator.RemoveControllerData(controllerCamera);
        cameraAnimator.ApplyController(controllerCamera);
        foreach (var anim in cameraAnims)
        {
            anim.layer = cameraAnimator.FindLayer(controllerCamera.Prefix + anim.name);
            anim.layer.Mix = 0;
            anim.layer.ActiveState.SetNormalizedTime(anim.startTimeNormalized);
            var mc = anim.layer.GetMotionControllerUnsafe(anim.layer.ActiveState.MotionControllerIndex);
            mc.BaseSpeed = 0;
        }

        maxTime = 0;
        foreach(var task in characterTasks)
        {
            foreach(var anim in task.animsToPlay)
            {
                var a = characterAnims[anim];
                maxTime = Mathf.Max(a.Length + task.startTime, maxTime);
            }
        }
        foreach (var task in cameraTasks)
        {
            foreach (var anim in task.animsToPlay)
            {
                var a = cameraAnims[anim];
                maxTime = Mathf.Max(a.Length + task.startTime, maxTime); 
            }
        }

        if (cameraTasks != null && cameraTasks.Length > 0)
        {
            cameraProxy = cameraAnimator.GetComponent<CameraProxy>();
            cameraProxy.SetTargetCamera(Camera.main);
            cameraProxy.IsInRelativeMode = false;
        }

        Debug.Log("max time: " + maxTime);
    }

    protected void Start()
    {
        Restart();
    }

    protected bool playing = false; 

    protected void Update()
    {
        if (play)
        {
            play = false; 
            Restart();
            playing = true;
        }
        if (stop)
        {
            stop = false;
            Stop();
        }

        if (!playing) return;
        float prevTime = time;
        time = time + Time.deltaTime;
        if (time > maxTime)
        {
            Stop();
            return;
        }

        foreach(var anim in characterAnims)
        {
            anim.layer.Mix = 0;
        }
        foreach (var anim in cameraAnims)
        {
            anim.layer.Mix = 0;
        }

        foreach(var task in characterTasks)
        {
            if (task.animsToPlay != null)
            {
                foreach (var index in task.animsToPlay)
                {
                    var anim = characterAnims[index];
                    if (time >= task.startTime)
                    {
                        anim.layer.ActiveState.SetTime(anim.layer.ActiveState.GetTime() + Time.deltaTime * anim.speed);
                        float fade = 1;
                        float normTime = anim.layer.ActiveState.GetNormalizedTime();
                        if (normTime < anim.fadeIn) fade = fade * Mathf.Clamp01(normTime / anim.fadeIn);
                        if (normTime > anim.fadeOut && anim.fadeOut > 0) fade = fade * (1 - Mathf.Clamp01((normTime - anim.fadeOut) / (1 - anim.fadeOut))); 
                        anim.layer.Mix = fade;
                    }
                }
            }
            if (task.soundsToPlay != null)
            {
                foreach(var index in task.soundsToPlay)
                {
                    if ((prevTime < task.startTime || (task.startTime <= 0 && prevTime <= 0)) && time >= task.startTime)
                    {
                        characterSounds[index].Play(time - task.startTime);  
                    }
                }
            }
        }
        foreach (var task in cameraTasks)
        {
            if (task.animsToPlay != null)
            {
                foreach (var index in task.animsToPlay)
                {
                    var anim = cameraAnims[index];
                    if (time >= task.startTime)
                    {
                        anim.layer.ActiveState.SetTime(anim.layer.ActiveState.GetTime() + Time.deltaTime * anim.speed);
                        float fade = 1;
                        float normTime = anim.layer.ActiveState.GetNormalizedTime();
                        if (normTime < anim.fadeIn) fade = fade * Mathf.Clamp01(normTime / anim.fadeIn);
                        if (normTime > anim.fadeOut && anim.fadeOut > 0) fade = fade * (1 - Mathf.Clamp01((normTime - anim.fadeOut) / (1 - anim.fadeOut)));
                        anim.layer.Mix = fade;
                    }
                }
            }
        }

        foreach (var anim in characterAnims)
        { 
            anim.layer.Mix = anim.layer.Mix * anim.GetMix(characterAnims);
        }
        foreach (var anim in cameraAnims)
        {
            anim.layer.Mix = anim.layer.Mix * anim.GetMix(cameraAnims);
        }

        if (cameraProxy != null) cameraProxy.ApplyChanges(false);  

    }

}
