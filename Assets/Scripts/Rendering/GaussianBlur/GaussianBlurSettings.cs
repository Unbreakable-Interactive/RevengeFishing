using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Visuals.PostProcessing
{
    [Serializable, VolumeComponentMenu("Gaussian Blur")]
    public class GaussianBlurSettings : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Standard deviation (spread) of the blur. Grid size is approx. 3x larger.")]
        public ClampedFloatParameter strength = new (0f, 0f, 15f);

        public bool IsActive()
        {
            return strength.value > 0f && active;
        }

        public bool IsTileCompatible()
        {
            return false;
        }
    }
}
