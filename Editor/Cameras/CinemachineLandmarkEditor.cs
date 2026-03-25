using Twinny.Editor.Navigation;
using Twinny.Multiplatform.Cameras;
using UnityEditor;
using UnityEngine.UIElements;

namespace Twinny.Multiplatform.Editor.Cameras
{
    [CustomEditor(typeof(CinemachineLandmark))]
    [CanEditMultipleObjects]
    public class CinemachineLandmarkEditor : LandmarkEditor
    {
        protected override string InspectorTitle => "Cinemachine Landmark";

        protected override string InspectorSubtitle => "Landmark ready for mobile navigation and camera overrides";

        protected override void AddAdditionalSections(VisualElement contentRoot)
        {
            AddCinemachineSection(CreateSection(contentRoot, "Cinemachine"));
        }

        private void AddCinemachineSection(VisualElement container)
        {
            if (container == null)
            {
                return;
            }

            AddHelpLabel(container, "Use this landmark as the mobile-specific extension point for future navigation and Cinemachine override fields.");
        }
    }
}
