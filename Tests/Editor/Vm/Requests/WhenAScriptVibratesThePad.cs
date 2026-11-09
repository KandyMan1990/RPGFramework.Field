using NUnit.Framework;

namespace RPGFramework.Field.Tests.Vm.Requests
{
    internal sealed class WhenAScriptVibratesThePad
    {
        [Test]
        public void ThenTheStrengthsAndTimeArePassedOn()
        {
            TestField field = new TestField();
            float[]   asked = null;

            field.Vm.RequestSetVibration += (low, high, seconds) => asked = new[] { low, high, seconds };

            field.Run(new Script().Op(FieldScriptOpCode.SetVibration)
                                  .Sources(Source.Immediate, Source.Immediate).Float(0.5f).Float(1f)
                                  .Sources(Source.Immediate, Source.Immediate).Float(2f).Return());

            Assert.AreEqual(new[] { 0.5f, 1f, 2f }, asked);
        }
    }
}
