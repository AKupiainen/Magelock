namespace MageLock.Editor
{
	using System.Collections.Generic;
	using System.Linq;
	using UnityEditor;
	using UnityEngine;

	public class ParticleHelperTools : EditorWindow
	{
		private float scaleMultiplier = 1f;

		private Color targetColor;
        private float hueDifference;
        private float saturationDifference;
        private float targetHue;
        private float targetSaturation;
        
		[MenuItem("Tools/Particle Helper Tools")]
		private static void ShowWindow()
		{
			GetWindow<ParticleHelperTools>().Show();
		}

		private void OnGUI()
		{
			GUILayout.BeginVertical();

			GUILayout.BeginHorizontal();

            scaleMultiplier = EditorGUILayout.Slider(scaleMultiplier, 0.01f, 4.0f);

			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();

			if (GUILayout.Button("Resize"))
			{
				foreach (GameObject selection in Selection.gameObjects)
				{
					foreach (ParticleSystem system in selection.GetComponentsInChildren<ParticleSystem>())
					{
						system.Stop();
						system.Clear(true);

						ScaleParticles(selection, system);
						system.Play();
					}
				}
			}

			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();

			targetColor = EditorGUILayout.ColorField("New Color", targetColor);

			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();

			if (GUILayout.Button("Change Color"))
			{
				List<ParticleSystem> particleSystems = Selection.activeGameObject.GetComponentsInChildren<ParticleSystem>().ToList();

                foreach (ParticleSystem particleSystem in particleSystems)
                {
                    HueShiftParticleSystem(particleSystem, targetColor);
                }
			}

			GUILayout.EndHorizontal();

			GUILayout.EndVertical();
		}

        private void ScaleParticles(GameObject parent, ParticleSystem particles) 
        {
		    if (parent != particles.gameObject) 
            {
			    particles.transform.localPosition *= scaleMultiplier;
		    }
            
		    SerializedObject serializedParticles = new(particles);

		    serializedParticles.FindProperty("InitialModule.gravityModifier.scalar").floatValue *= scaleMultiplier;
		    serializedParticles.FindProperty("InitialModule.gravityModifier.minScalar").floatValue *= scaleMultiplier;
            
		    serializedParticles.FindProperty("NoiseModule.strength.scalar").floatValue *= scaleMultiplier;
		    serializedParticles.FindProperty("NoiseModule.strength.minScalar").floatValue *= scaleMultiplier;
		    serializedParticles.FindProperty("NoiseModule.strengthY.scalar").floatValue *= scaleMultiplier;
		    serializedParticles.FindProperty("NoiseModule.strengthY.minScalar").floatValue *= scaleMultiplier;
		    serializedParticles.FindProperty("NoiseModule.strengthZ.scalar").floatValue *= scaleMultiplier;
		    serializedParticles.FindProperty("NoiseModule.strengthZ.minScalar").floatValue *= scaleMultiplier;
		    serializedParticles.FindProperty("NoiseModule.frequency").floatValue /= scaleMultiplier;

		    serializedParticles.FindProperty("NoiseModule.sizeAmount.scalar").floatValue *= scaleMultiplier;
		    serializedParticles.FindProperty("NoiseModule.sizeAmount.minScalar").floatValue *= scaleMultiplier;
		    ScaleAnimationCurve(serializedParticles.FindProperty("NoiseModule.sizeAmount.minCurve").animationCurveValue);
		    ScaleAnimationCurve(serializedParticles.FindProperty("NoiseModule.sizeAmount.maxCurve").animationCurveValue);
            
		    serializedParticles.FindProperty("NoiseModule.rotationAmount.scalar").floatValue *= scaleMultiplier;
		    serializedParticles.FindProperty("NoiseModule.rotationAmount.minScalar").floatValue *= scaleMultiplier;
		    ScaleAnimationCurve(serializedParticles.FindProperty("NoiseModule.rotationAmount.minCurve").animationCurveValue);
		    ScaleAnimationCurve(serializedParticles.FindProperty("NoiseModule.rotationAmount.maxCurve").animationCurveValue);
            
		    serializedParticles.FindProperty("NoiseModule.positionAmount.scalar").floatValue *= scaleMultiplier;
		    serializedParticles.FindProperty("NoiseModule.positionAmount.minScalar").floatValue *= scaleMultiplier;
		    ScaleAnimationCurve(serializedParticles.FindProperty("NoiseModule.positionAmount.minCurve").animationCurveValue);
		    ScaleAnimationCurve(serializedParticles.FindProperty("NoiseModule.positionAmount.maxCurve").animationCurveValue);

		    serializedParticles.FindProperty("LightsModule.rangeCurve.scalar").floatValue *= scaleMultiplier;
		    serializedParticles.FindProperty("LightsModule.rangeCurve.minScalar").floatValue *= scaleMultiplier;
            
		    serializedParticles.FindProperty("InitialModule.startSize.scalar").floatValue *= scaleMultiplier;
		    serializedParticles.FindProperty("InitialModule.startSize.minScalar").floatValue *= scaleMultiplier;
		    serializedParticles.FindProperty("InitialModule.startSizeY.scalar").floatValue *= scaleMultiplier;
		    serializedParticles.FindProperty("InitialModule.startSizeY.minScalar").floatValue *= scaleMultiplier;
		    serializedParticles.FindProperty("InitialModule.startSizeZ.scalar").floatValue *= scaleMultiplier;
		    serializedParticles.FindProperty("InitialModule.startSizeZ.minScalar").floatValue *= scaleMultiplier;
            
		    ScaleAnimationCurve(serializedParticles.FindProperty("InitialModule.startSize.minCurve").animationCurveValue);
		    ScaleAnimationCurve(serializedParticles.FindProperty("InitialModule.startSize.maxCurve").animationCurveValue);
		    ScaleAnimationCurve(serializedParticles.FindProperty("InitialModule.startSizeY.minCurve").animationCurveValue);
		    ScaleAnimationCurve(serializedParticles.FindProperty("InitialModule.startSizeY.maxCurve").animationCurveValue);
		    ScaleAnimationCurve(serializedParticles.FindProperty("InitialModule.startSizeZ.minCurve").animationCurveValue);
		    ScaleAnimationCurve(serializedParticles.FindProperty("InitialModule.startSizeZ.maxCurve").animationCurveValue);
            
		    serializedParticles.FindProperty("InitialModule.startSpeed.scalar").floatValue *= scaleMultiplier;
		    serializedParticles.FindProperty("InitialModule.startSpeed.minScalar").floatValue *= scaleMultiplier;	

		    serializedParticles.FindProperty("VelocityModule.x.minScalar").floatValue *= scaleMultiplier;
		    serializedParticles.FindProperty("VelocityModule.y.minScalar").floatValue *= scaleMultiplier;
		    serializedParticles.FindProperty("VelocityModule.z.minScalar").floatValue *= scaleMultiplier;

		    serializedParticles.FindProperty("ClampVelocityModule.x.minScalar").floatValue *= scaleMultiplier;
		    serializedParticles.FindProperty("ClampVelocityModule.y.minScalar").floatValue *= scaleMultiplier;
		    serializedParticles.FindProperty("ClampVelocityModule.z.minScalar").floatValue *= scaleMultiplier;

		    serializedParticles.FindProperty("ForceModule.x.minScalar").floatValue *= scaleMultiplier;
		    serializedParticles.FindProperty("ForceModule.y.minScalar").floatValue *= scaleMultiplier;
		    serializedParticles.FindProperty("ForceModule.z.minScalar").floatValue *= scaleMultiplier;
            
		    serializedParticles.FindProperty("ClampVelocityModule.magnitude.minScalar").floatValue *= scaleMultiplier;
		    serializedParticles.FindProperty("ShapeModule.m_Scale").vector3Value *= scaleMultiplier;

		    serializedParticles.FindProperty("ShapeModule.radius.value").floatValue *= scaleMultiplier;

		    serializedParticles.FindProperty("VelocityModule.x.scalar").floatValue *= scaleMultiplier;
		    serializedParticles.FindProperty("VelocityModule.y.scalar").floatValue *= scaleMultiplier;
		    serializedParticles.FindProperty("VelocityModule.z.scalar").floatValue *= scaleMultiplier;

		    ScaleAnimationCurve(serializedParticles.FindProperty("VelocityModule.x.minCurve").animationCurveValue);
		    ScaleAnimationCurve(serializedParticles.FindProperty("VelocityModule.x.maxCurve").animationCurveValue);
		    ScaleAnimationCurve(serializedParticles.FindProperty("VelocityModule.y.minCurve").animationCurveValue);
		    ScaleAnimationCurve(serializedParticles.FindProperty("VelocityModule.y.maxCurve").animationCurveValue);
		    ScaleAnimationCurve(serializedParticles.FindProperty("VelocityModule.z.minCurve").animationCurveValue);
		    ScaleAnimationCurve(serializedParticles.FindProperty("VelocityModule.z.maxCurve").animationCurveValue);

		    serializedParticles.FindProperty("ClampVelocityModule.x.scalar").floatValue *= scaleMultiplier;
		    serializedParticles.FindProperty("ClampVelocityModule.y.scalar").floatValue *= scaleMultiplier;
		    serializedParticles.FindProperty("ClampVelocityModule.z.scalar").floatValue *= scaleMultiplier;
		    serializedParticles.FindProperty("ClampVelocityModule.magnitude.scalar").floatValue *= scaleMultiplier;

		    ScaleAnimationCurve(serializedParticles.FindProperty("ClampVelocityModule.x.minCurve").animationCurveValue);
		    ScaleAnimationCurve(serializedParticles.FindProperty("ClampVelocityModule.x.maxCurve").animationCurveValue);
		    ScaleAnimationCurve(serializedParticles.FindProperty("ClampVelocityModule.y.minCurve").animationCurveValue);
		    ScaleAnimationCurve(serializedParticles.FindProperty("ClampVelocityModule.y.maxCurve").animationCurveValue);
		    ScaleAnimationCurve(serializedParticles.FindProperty("ClampVelocityModule.z.minCurve").animationCurveValue);
		    ScaleAnimationCurve(serializedParticles.FindProperty("ClampVelocityModule.z.maxCurve").animationCurveValue);
		    ScaleAnimationCurve(serializedParticles.FindProperty("ClampVelocityModule.magnitude.minCurve").animationCurveValue);
		    ScaleAnimationCurve(serializedParticles.FindProperty("ClampVelocityModule.magnitude.maxCurve").animationCurveValue);

		    serializedParticles.FindProperty("ForceModule.x.scalar").floatValue *= scaleMultiplier;
		    serializedParticles.FindProperty("ForceModule.y.scalar").floatValue *= scaleMultiplier;
		    serializedParticles.FindProperty("ForceModule.z.scalar").floatValue *= scaleMultiplier;

            ScaleAnimationCurve(serializedParticles.FindProperty("ForceModule.x.minCurve").animationCurveValue);
            ScaleAnimationCurve(serializedParticles.FindProperty("ForceModule.x.maxCurve").animationCurveValue);
		    ScaleAnimationCurve(serializedParticles.FindProperty("ForceModule.y.minCurve").animationCurveValue);
		    ScaleAnimationCurve(serializedParticles.FindProperty("ForceModule.y.maxCurve").animationCurveValue);
		    ScaleAnimationCurve(serializedParticles.FindProperty("ForceModule.z.minCurve").animationCurveValue);
		    ScaleAnimationCurve(serializedParticles.FindProperty("ForceModule.z.maxCurve").animationCurveValue);
		    serializedParticles.ApplyModifiedProperties();
	    }
        
		private void ScaleAnimationCurve(AnimationCurve curve)
		{
			for (int i = 0; i < curve.keys.Length; i++)
			{
				Keyframe tempKey = curve.keys[i];
				tempKey.value *= scaleMultiplier;
				curve.keys[i] = tempKey;
			}
		}

        private void HueShiftParticleSystem(ParticleSystem particleSystem, Color targetColor, int gradientColorKeyIndex = 0)
        {
            ParticleSystem.MainModule mainModule = particleSystem.main;
            Color.RGBToHSV(targetColor, out targetHue, out targetSaturation, out float _);

            switch(mainModule.startColor.mode)
            {
                case ParticleSystemGradientMode.Color:
                    mainModule.startColor = new ParticleSystem.MinMaxGradient(ShiftColorAndSaveDif(mainModule.startColor.color));
                    break;

                case ParticleSystemGradientMode.Gradient:
                case ParticleSystemGradientMode.RandomColor:
                    Gradient shiftedGradient = GradientShiftAndSaveDif(mainModule.startColor.gradient, true, gradientColorKeyIndex);
                    mainModule.startColor = shiftedGradient;
                    break;

                case ParticleSystemGradientMode.TwoColors:
                    mainModule.startColor = new ParticleSystem.MinMaxGradient(ShiftColorAndSaveDif(mainModule.startColor.colorMax),
                        ShiftColorFromPreCalculatedDif(mainModule.startColor.colorMin));
                    break;

                case ParticleSystemGradientMode.TwoGradients:
                    Gradient shiftedGradientMax = GradientShiftAndSaveDif(mainModule.startColor.gradientMax, true, 0);
                    Gradient shiftedGradientMin = GradientShiftFromPreCalculatedDif(mainModule.startColor.gradientMin);
                    mainModule.startColor = new ParticleSystem.MinMaxGradient(shiftedGradientMax, shiftedGradientMin);
                    break;
            }

            
            ParticleSystem.ColorOverLifetimeModule colorOverLifetimeModule = particleSystem.colorOverLifetime;
            
            if(colorOverLifetimeModule.enabled)
            {
                switch(colorOverLifetimeModule.color.mode)
                {
                    case ParticleSystemGradientMode.Color:
                        colorOverLifetimeModule.color = new ParticleSystem.MinMaxGradient(ShiftColorAndSaveDif(colorOverLifetimeModule.color.color));
                        break;

                    case ParticleSystemGradientMode.Gradient:
                    case ParticleSystemGradientMode.RandomColor:
                        Gradient shiftedGradient = GradientShiftAndSaveDif(colorOverLifetimeModule.color.gradient, true, gradientColorKeyIndex);
                        colorOverLifetimeModule.color = shiftedGradient;
                        break;

                    case ParticleSystemGradientMode.TwoColors:
                        colorOverLifetimeModule.color = new ParticleSystem.MinMaxGradient(ShiftColorAndSaveDif(colorOverLifetimeModule.color.colorMax),
                            ShiftColorFromPreCalculatedDif(colorOverLifetimeModule.color.colorMin));
                        break;

                    case ParticleSystemGradientMode.TwoGradients:
                        Gradient shiftedGradientMax = GradientShiftAndSaveDif(colorOverLifetimeModule.color.gradientMax, true, 0);
                        Gradient shiftedGradientMin = GradientShiftFromPreCalculatedDif(colorOverLifetimeModule.color.gradientMin);
                        colorOverLifetimeModule.color = new ParticleSystem.MinMaxGradient(shiftedGradientMax, shiftedGradientMin);
                        break;
                }
            }
            
            ParticleSystem.ColorBySpeedModule colorBySpeedModule = particleSystem.colorBySpeed;
            
            if(colorBySpeedModule.enabled)
            {
                switch(colorBySpeedModule.color.mode)
                {
                    case ParticleSystemGradientMode.Color:
                        colorBySpeedModule.color = new ParticleSystem.MinMaxGradient(ShiftColorAndSaveDif(colorBySpeedModule.color.color));
                        break;

                    case ParticleSystemGradientMode.Gradient:
                    case ParticleSystemGradientMode.RandomColor:
                        Gradient shiftedGradient = GradientShiftAndSaveDif(colorBySpeedModule.color.gradient, true, gradientColorKeyIndex);
                        colorBySpeedModule.color= shiftedGradient;
                        break;

                    case ParticleSystemGradientMode.TwoColors:
                        colorBySpeedModule.color = new ParticleSystem.MinMaxGradient(ShiftColorAndSaveDif(colorBySpeedModule.color.colorMax),
                            ShiftColorFromPreCalculatedDif(colorBySpeedModule.color.colorMin));
                        break;

                    case ParticleSystemGradientMode.TwoGradients:
                        Gradient shiftedGradientMax = GradientShiftAndSaveDif(colorBySpeedModule.color.gradientMax, true, 0);
                        Gradient shiftedGradientMin = GradientShiftFromPreCalculatedDif(colorBySpeedModule.color.gradientMin);
                        colorBySpeedModule.color = new ParticleSystem.MinMaxGradient(shiftedGradientMax, shiftedGradientMin);
                        break;
                }
            }
        }
        
        private Gradient GradientShiftAndSaveDif(Gradient inGradient, bool saveColorOfFirstKey = false, int indexOfReferenceColor = 0)
        {
            GradientColorKey[] colorKeys = new GradientColorKey[inGradient.colorKeys.Length];
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[inGradient.alphaKeys.Length];
            for(int i = 0; i < colorKeys.Length; i++)
            {
                if(i == indexOfReferenceColor && saveColorOfFirstKey)
                {
                    colorKeys[i].color = ShiftColorAndSaveDif(inGradient.colorKeys[i].color);
                }
                else
                {
                    colorKeys[i].color = ShiftColorFromPreCalculatedDif(inGradient.colorKeys[i].color);
                }

                colorKeys[i].time = inGradient.colorKeys[i].time;
            }

            for(int i = 0; i < alphaKeys.Length; i++)
            {
                alphaKeys[i].alpha = inGradient.alphaKeys[i].alpha;
                alphaKeys[i].time = inGradient.alphaKeys[i].time;
            }

            Gradient gradient = new();
            gradient.SetKeys(colorKeys, alphaKeys);
            return gradient;
        }
        
        private Gradient GradientShiftFromPreCalculatedDif(Gradient inGradient)
        {
            GradientColorKey[] colorKeys = new GradientColorKey[inGradient.colorKeys.Length];
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[inGradient.alphaKeys.Length];
            
            for(int i = 0; i < colorKeys.Length; i++)
            {
                colorKeys[i].color = ShiftColorFromPreCalculatedDif(inGradient.colorKeys[i].color);
                colorKeys[i].time = inGradient.colorKeys[i].time;
            }

            for(int i = 0; i < alphaKeys.Length; i++)
            {
                alphaKeys[i].alpha = inGradient.alphaKeys[i].alpha;
                alphaKeys[i].time = inGradient.alphaKeys[i].time;
            }

            Gradient gradient = new();
            gradient.SetKeys(colorKeys, alphaKeys);
            return gradient;
        }

        private Color ShiftColorAndSaveDif(Color colorToShift)
        {
            Color.RGBToHSV(colorToShift, out float colorHue, out float colorSaturation, out float colorValue);
            hueDifference = targetHue - colorHue;
            saturationDifference = targetSaturation - colorSaturation;
            return Color.HSVToRGB(Mathf.Clamp01(colorHue + hueDifference), Mathf.Clamp01(colorSaturation + saturationDifference), colorValue);
        }

        private Color ShiftColorFromPreCalculatedDif(Color colorToShift)
        {
            Color.RGBToHSV(colorToShift, out float colorHue, out float colorSaturation, out float colorValue);
            return Color.HSVToRGB(Mathf.Clamp01(colorHue + hueDifference), Mathf.Clamp01(colorSaturation + saturationDifference), colorValue);
        }
	}
}