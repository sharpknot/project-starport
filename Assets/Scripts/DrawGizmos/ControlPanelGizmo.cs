using Drawing;
using UnityEngine;

namespace Starport.DrawGizmos
{
    [RequireComponent(typeof(ControlPanelController))]
    public class ControlPanelGizmo : MonoBehaviourGizmos
    {
        protected ControlPanelController ControlPanel
        {
            get
            {
                if (_controlPanel == null)
                    _controlPanel = GetComponent<ControlPanelController>();
                return _controlPanel;
            }
        }
        private ControlPanelController _controlPanel;

        public override void DrawGizmos()
        {
            base.DrawGizmos();

            float capsuleRadius = 0.5f;
            float capsuleHeight = 2f;

            Draw.WireCapsule(ControlPanel.Seat.position, ControlPanel.Seat.up, capsuleHeight, capsuleRadius, Color.aliceBlue);
            Draw.Arrow(ControlPanel.Seat.position, ControlPanel.Seat.position + ControlPanel.Seat.forward, ControlPanel.Seat.up, capsuleRadius, Color.blue);
            
        }
    }
}
