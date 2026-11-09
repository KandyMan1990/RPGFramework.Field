using NUnit.Framework;
using UnityEngine;
using UnityEngine.VFX;

namespace RPGFramework.Field.Tests.Effects
{
    /// <summary>An effect with nothing under it but a child, or a Particle System where a test adds one.</summary>
    internal sealed class WhenAnEffectIsSwitched
    {
        private GameObject  m_Object;
        private FieldEffect m_Effect;

        [SetUp]
        public void SetUp()
        {
            m_Object = new GameObject("Effect");
            m_Effect = m_Object.AddComponent<FieldEffect>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Object);
        }

        [Test]
        public void ThenOneWithNoParticlesGoesOffAtOnce()
        {
            m_Effect.Bind();
            m_Effect.SwitchOff();

            Assert.IsFalse(m_Object.activeSelf);
        }

        [Test]
        public void ThenSwitchingOnBringsItBack()
        {
            m_Effect.Bind();
            m_Effect.SwitchOff();
            m_Effect.SwitchOn();

            Assert.IsTrue(m_Object.activeSelf);
        }

        [Test]
        public void ThenParticlesAlreadyOutAreLeftToFinish()
        {
            Emit();

            m_Effect.SwitchOff();
            m_Effect.Tick();

            Assert.IsTrue(m_Object.activeSelf);
        }

        [Test]
        public void ThenItGoesOffWhenTheLastHasGone()
        {
            ParticleSystem particles = Emit();

            m_Effect.SwitchOff();

            particles.Clear();

            m_Effect.Tick();

            Assert.IsFalse(m_Object.activeSelf);
        }

        [Test]
        public void ThenSwitchingOnWhileFinishingKeepsItOn()
        {
            ParticleSystem particles = Emit();

            m_Effect.SwitchOff();
            m_Effect.SwitchOn();

            particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);

            m_Effect.Tick();

            Assert.IsTrue(m_Object.activeSelf);
        }

        [Test]
        public void ThenAGraphWithNothingAwakeGoesOffAtOnce()
        {
            m_Object.AddComponent<VisualEffect>();

            m_Effect.Bind();
            m_Effect.SwitchOff();

            Assert.IsFalse(m_Object.activeSelf);
        }

        [Test]
        public void ThenAtOnceLeavesNothingToFinish()
        {
            Emit();

            m_Effect.SwitchOffAtOnce();

            Assert.IsFalse(m_Object.activeSelf);
        }

        private ParticleSystem Emit()
        {
            ParticleSystem particles = m_Object.AddComponent<ParticleSystem>();

            m_Effect.Bind();

            particles.Emit(1);

            return particles;
        }
    }
}
