using System;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RPGFramework.Field.Tests.Facing
{
    /// <summary>A body is given the rotation its driver says faces along a direction, as a turn over time ends on.</summary>
    internal sealed class WhenAnEntityIsAimedAlongADirection
    {
        private GameObject m_Body;

        [SetUp]
        public void SetUp()
        {
            m_Body = new GameObject("Body");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Body);
        }

        [Test]
        public void ThenA3DRigidbodyFacesThatWay()
        {
            m_Body.AddComponent<Rigidbody>().isKinematic = true;

            AssertFaces(FieldDimension.ThreeD, new Vector3(1f, 0f, -1f));
        }

        [Test]
        public void ThenA2DRigidbodyFacesThatWay()
        {
            m_Body.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;

            AssertFaces(FieldDimension.TwoD, new Vector3(-1f, 1f, 0f));
        }

        [Test]
        public void ThenA3DTransformFacesThatWay()
        {
            AssertFaces(FieldDimension.ThreeD, new Vector3(-1f, 0f, 1f));
        }

        [Test]
        public void ThenA2DTransformFacesThatWay()
        {
            AssertFaces(FieldDimension.TwoD, new Vector3(1f, -1f, 0f));
        }

        private void AssertFaces(FieldDimension dimension, Vector3 direction)
        {
            IMovementDriver driver = MovementDriverFactory.Create(m_Body, 1f, Array.Empty<FieldBlocker>(), dimension, null);

            driver.SetRotation(driver.RotationAlong(direction));

            FacingAssert.Faces(direction, driver.Facing);
        }
    }
}
