using com.zibra.smoke_and_fire.Manipulators;
using UnityEditor;

namespace com.zibra.smoke_and_fire.Editor.Solver
{
    internal class ZibraSmokeAndFireManipulatorEditor : UnityEditor.Editor
    {
        protected void TriggerRepaint()
        {
            Repaint();
        }

        protected void OnEnable()
        {
            Manipulator manipulator = target as Manipulator;
            manipulator.OnChanged += TriggerRepaint;
        }

        protected void OnDisable()
        {
            Manipulator manipulator = target as Manipulator;
            manipulator.OnChanged -= TriggerRepaint;
        }
    }
}
