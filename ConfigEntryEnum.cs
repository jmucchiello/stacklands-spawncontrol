using UnityEngine;

namespace CommonModNS
{
    public class ConfigEntryEnum<T> : ConfigEntryModalHelper where T : Enum
    {
        private int content; // access via BoxedValue
        private int defaultValue; // access via BoxedValue
        private CustomButton anchor;  // this holds the ModOptionsScreen text that is clicked to open the menu

        public delegate string OnDisplayAnchorText();       // the text seen in the main option screen
        public delegate string OnDisplayAnchorTooltip();       // the text seen in the main option screen
        public delegate string OnDisplayEnumText(T t);
        public delegate string OnDisplayEnumTooltip(T t);
        public OnDisplayAnchorText onDisplayAnchorText;
        public OnDisplayAnchorTooltip onDisplayAnchorTooltip;
        public OnDisplayEnumText onDisplayEnumText;
        public OnDisplayEnumTooltip onDisplayEnumTooltip;

        public delegate bool OnChange(T newValue); // return false to prevent acceptance of newValue
        public OnChange onChange;

        public string popupMenuTitleText; // the title bar text of the popup screen
        public string popupMenuHelpText; // the help text that appears below the title bar text

        public string CloseButtonTextTerm = null; // if null, no close button is created
        public Color currentValueColor = Color.black;

        public virtual T DefaultValue { get => (T)(object)defaultValue; set => defaultValue = (int)(object)value; }
        public virtual T Value { get => (T)(object)content; set => content = (int)(object)value; }

        public override object BoxedValue
        {
            get => content;
            set => content = (int)value;
        }

        public ConfigEntryEnum(string name, ConfigFile configFile, T defaultValue, ConfigUI ui = null)
        {
            Name = name;
            ValueType = typeof(System.Object); // to avoid shenanigans from ModOptionScreen's default processing of string/int/bool
            DefaultValue = defaultValue;
            Config = configFile;
            if (Config.Data.TryGetValue(name, out _))
            {
                BoxedValue = Config.GetValue<int>(name); // store as int to make it easier to reload.
            }
            else
            {
                BoxedValue = defaultValue;
            }
            UI = new ConfigUI()
            {
                Hidden = true,
                Name = ui?.Name,
                NameTerm = ui?.NameTerm ?? name,
                Tooltip = ui?.Tooltip,
                TooltipTerm = ui?.TooltipTerm,
                PlaceholderText = ui?.PlaceholderText,
                RestartAfterChange = ui?.RestartAfterChange ?? false,
                ExtraData = ui?.ExtraData,
                OnUI = delegate (ConfigEntryBase c)
                {
                    anchor = DefaultButton(I.MOS.ButtonsParent,
                                           onDisplayAnchorText != null ? onDisplayAnchorText() : c.UI.GetName(),
                                           onDisplayAnchorTooltip != null ? onDisplayAnchorTooltip() : c.UI.GetTooltip());
                    anchor.Clicked += delegate
                    {
                        OpenMenu();
                    };
                }
            };
            configFile.Entries.Add(this);
        }

        private string EntryText(T entry)
        {
            string text = onDisplayEnumText != null ? onDisplayEnumText(entry) : Enum.GetName(typeof(T), entry);
            if (currentValueColor != null && EqualityComparer<T>.Default.Equals(entry, (T)BoxedValue))
            {
                text = ColorText(currentValueColor, text);
            }
            return text;
        }

        private string EntryTooltip(T entry)
        {
            return onDisplayEnumTooltip != null ? onDisplayEnumTooltip(entry) : null;
        }

        private void OpenMenu()
        {
            if (GameCanvas.instance.ModalIsOpen) return;
            ModalScreen.instance.Clear();
            popup = ModalScreen.instance;
            popup.SetTexts(I.Xlat(popupMenuTitleText), I.Xlat(popupMenuHelpText));
            foreach (T t in Enum.GetValues(typeof(T)))
            {
                T thisEntry = t; // so the delegate grabs the correct value, not the loop variable
                CustomButton btn = DefaultButton(popup.ButtonParent,
                                                 EntryText(thisEntry),
                                                 EntryTooltip(thisEntry));
                btn.Clicked += delegate ()
                {
                    if (onChange == null || onChange(thisEntry))
                    {
                        Config.Data[Name] = (int)(object)thisEntry;
                        content = (int)(object)thisEntry;
                        anchor.TextMeshPro.text = onDisplayAnchorText != null ? onDisplayAnchorText() : UI.GetName();
                        CloseMenu();
                    }
                };
            }
            if (CloseButtonTextTerm != null)
            {
                CustomButton btnClose = DefaultButton(ModalScreen.instance.ButtonParent, RightAlign(I.Xlat(CloseButtonTextTerm)));
                btnClose.Clicked += CloseMenu;
            }
            GameCanvas.instance.OpenModal();
        }

        public override void SetDefaults()
        {
            Config.Data[Name] = content = defaultValue;
            anchor.TextMeshPro.text = onDisplayAnchorText != null ? onDisplayAnchorText() : UI.GetName();
            anchor.TooltipText = onDisplayAnchorTooltip != null ? onDisplayAnchorTooltip() : UI.GetTooltip();
        }
    }
}
