using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CommonModNS
{
    // Base class for Configuration Entries that open a modal dialog.
    public abstract class ConfigEntryModalHelper : ConfigEntryHelper
    {
        protected static ModalScreen popup;
        protected CustomButton AnchorButton;

        public void CloseMenu()
        {
            GameCanvas.instance.CloseModal();
            popup = null;
        }
    }
}
