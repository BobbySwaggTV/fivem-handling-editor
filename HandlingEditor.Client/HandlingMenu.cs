﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.UI;
using static CitizenFX.Core.Native.API;
using MenuAPI;

namespace HandlingEditor.Client
{
    public class HandlingMenu : BaseScript
    {
        #region Editor Properties

        public string ScriptName => HandlingEditor.ScriptName;
        public string kvpPrefix => HandlingEditor.kvpPrefix;
        public float FloatStep => HandlingEditor.FloatStep;
        public int ToggleMenu => HandlingEditor.ToggleMenu;
        public bool CurrentPresetIsValid => HandlingEditor.CurrentPresetIsValid;
        public HandlingPreset CurrentPreset => HandlingEditor.CurrentPreset;
        public Dictionary<string, HandlingPreset> ServerPresets => HandlingEditor.ServerPresets;

        #endregion

        #region Private Fields

        private MenuController menuController;
        private Menu EditorMenu;
        private Menu PersonalPresetsMenu;
        private Menu ServerPresetsMenu;

        #endregion

        #region Delegates

        public delegate void EditorMenuPresetValueChangedEvent(string id, string value, string text);

        #endregion

        #region Public Events

        public static event EditorMenuPresetValueChangedEvent MenuPresetValueChanged;

        public static event EventHandler MenuResetPresetButtonPressed;
        public static event EventHandler<string> MenuApplyPersonalPresetButtonPressed;
        public static event EventHandler<string> MenuApplyServerPresetButtonPressed;
        public static event EventHandler<string> MenuSavePersonalPresetButtonPressed;
        public static event EventHandler<string> MenuSaveServerPresetButtonPressed;
        public static event EventHandler<string> MenuDeletePersonalPresetButtonPressed;
        public static event EventHandler<string> MenuDeleteServerPresetButtonPressed;

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public HandlingMenu()
        {
            // Used for the on screen keyboard
            AddTextEntry("HANDLING_EDITOR_ENTER_VALUE", "Enter value (without spaces)");
            InitializeMenu();

            Tick += OnTick;
            HandlingEditor.PresetChanged += new EventHandler((sender,args) => UpdateEditorMenu());
            HandlingEditor.PersonalPresetsListChanged += new EventHandler((sender,args) => UpdatePersonalPresetsMenu());
            HandlingEditor.ServerPresetsListChanged += new EventHandler((sender,args) => UpdateServerPresetsMenu());
        }

        #endregion

        #region Tasks
        
        /// <summary>
        /// The task that checks if the menu can be open
        /// </summary>
        /// <returns></returns>
        private async Task OnTick()
        {
            if (!CurrentPresetIsValid)
            {
                if (MenuController.IsAnyMenuOpen())
                    MenuController.CloseAllMenus();
            }

            await Task.FromResult(0);
        }
        
        #endregion

        #region Private Methods

        /// <summary>
        /// Setup the Menu to be used with the script
        /// </summary>
        private void InitializeMenu()
        {
            if (EditorMenu == null)
            {
                EditorMenu = new Menu(ScriptName, "Editor");

                EditorMenu.OnItemSelect += EditorMenu_OnItemSelect;
                EditorMenu.OnDynamicListItemSelect += EditorMenu_OnDynamicListItemSelect;
                EditorMenu.OnDynamicListItemCurrentItemChange += EditorMenu_OnDynamicListItemCurrentItemChange;
            }
            
            if (PersonalPresetsMenu == null)
            {
                PersonalPresetsMenu = new Menu(ScriptName, "Personal Presets");

                PersonalPresetsMenu.OnItemSelect += PersonalPresetsMenu_OnItemSelect;

                #region Save/Delete Handler

                PersonalPresetsMenu.InstructionalButtons.Add(Control.PhoneExtraOption, GetLabelText("ITEM_SAVE"));
                PersonalPresetsMenu.InstructionalButtons.Add(Control.PhoneOption, GetLabelText("ITEM_DEL"));

                // Disable Controls binded on the same key
                PersonalPresetsMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler(Control.SelectWeapon, Menu.ControlPressCheckType.JUST_PRESSED, new Action<Menu, Control>((sender, control) => { }), true));
                PersonalPresetsMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler(Control.VehicleExit, Menu.ControlPressCheckType.JUST_PRESSED, new Action<Menu, Control>((sender, control) => { }), true));

                PersonalPresetsMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler(Control.PhoneExtraOption, Menu.ControlPressCheckType.JUST_PRESSED, new Action<Menu, Control>(async (sender, control) =>
                {
                    string kvpName = await GetOnScreenString("");
                    MenuSavePersonalPresetButtonPressed?.Invoke(PersonalPresetsMenu, kvpName);
                }), true));
                PersonalPresetsMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler(Control.PhoneOption, Menu.ControlPressCheckType.JUST_PRESSED, new Action<Menu, Control>((sender, control) =>
                {
                    if (PersonalPresetsMenu.GetMenuItems().Count > 0)
                    {
                        string kvpName = PersonalPresetsMenu.GetMenuItems()[PersonalPresetsMenu.CurrentIndex].Text;
                        MenuDeletePersonalPresetButtonPressed?.Invoke(PersonalPresetsMenu, kvpName);
                    }
                }), true));

                #endregion
            }
            if (ServerPresetsMenu == null)
            {
                ServerPresetsMenu = new Menu(ScriptName, "Server Presets");

                ServerPresetsMenu.OnItemSelect += ServerPresetsMenu_OnItemSelect;
            }

            UpdatePersonalPresetsMenu();
            UpdateServerPresetsMenu();
            UpdateEditorMenu();

            if (menuController == null)
            {
                menuController = new MenuController();
                MenuController.AddMenu(EditorMenu);
                MenuController.AddMenu(PersonalPresetsMenu);
                MenuController.AddMenu(ServerPresetsMenu);
                MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Right;
                MenuController.MenuToggleKey = (Control)ToggleMenu;
                MenuController.EnableMenuToggleKeyOnController = false;
                MenuController.MainMenu = EditorMenu;
            }
        }

        /// <summary>
        /// Invoked when the an item from the personal presets menu is selected
        /// </summary>
        /// <param name="menu"></param>
        /// <param name="menuItem"></param>
        /// <param name="itemIndex"></param>
        private void PersonalPresetsMenu_OnItemSelect(Menu menu, MenuItem menuItem, int itemIndex) => MenuApplyPersonalPresetButtonPressed?.Invoke(menu, menuItem.Text);

        /// <summary>
        /// Invoked when the an item from the server presets menu is selected
        /// </summary>
        /// <param name="menu"></param>
        /// <param name="menuItem"></param>
        /// <param name="itemIndex"></param>
        private void ServerPresetsMenu_OnItemSelect(Menu menu, MenuItem menuItem, int itemIndex) => MenuApplyServerPresetButtonPressed?.Invoke(menu, menuItem.Text);

        /// <summary>
        /// Invoked when an item from the main editor menu is selected
        /// </summary>
        /// <param name="menu"></param>
        /// <param name="menuItem"></param>
        /// <param name="itemIndex"></param>
        private void EditorMenu_OnItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
        {
            // If the sender isn't the main editor menu...
            if (menu != EditorMenu)
                return;

            if ((menuItem.ItemData as string) == "handling_reset")
            {
                MenuResetPresetButtonPressed?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Invoked when a <see cref="MenuDynamicListItem"/> from the main editor menu is selected
        /// </summary>
        /// <param name="menu"></param>
        /// <param name="dynamicListItem"></param>
        /// <param name="currentItem"></param>
        private async void EditorMenu_OnDynamicListItemSelect(Menu menu, MenuDynamicListItem dynamicListItem, string currentItem)
        {
            // If the item doesn't control any preset field...
            if (!(dynamicListItem.ItemData is BaseFieldInfo fieldInfo))
                return;

            //var currentItem = dynamicListItem.CurrentItem;
            var itemText = dynamicListItem.Text;
            string fieldName = fieldInfo.Name;
            var fieldType = fieldInfo.Type;

            // Get the user input value
            string text = await GetOnScreenString(currentItem);

            // Check if the value can be accepted
            if (fieldType == FieldType.FloatType)
            {
                var min = (fieldInfo as FieldInfo<float>).Min;
                var max = (fieldInfo as FieldInfo<float>).Max;

                if (float.TryParse(text, out float newvalue))
                {
                    if (newvalue >= min && newvalue <= max)
                    {
                        dynamicListItem.CurrentItem = newvalue.ToString();
                        // Notify the value is changed so the preset can update...
                        MenuPresetValueChanged?.Invoke(fieldName, newvalue.ToString("F3"), itemText);
                    }
                    else
                        Screen.ShowNotification($"{ScriptName}: Value out of allowed limits for ~b~{fieldName}~w~, Min:{min}, Max:{max}");
                }
                else
                    Screen.ShowNotification($"{ScriptName}: Invalid value for ~b~{fieldName}~w~");
            }
            else if (fieldType == FieldType.IntType)
            {
                var min = (fieldInfo as FieldInfo<int>).Min;
                var max = (fieldInfo as FieldInfo<int>).Max;

                if (int.TryParse(text, out int newvalue))
                {
                    if (newvalue >= min && newvalue <= max)
                    {
                        dynamicListItem.CurrentItem = newvalue.ToString();
                        // Notify the value is changed so the preset can update...
                        MenuPresetValueChanged?.Invoke(fieldName, newvalue.ToString(), itemText);
                    }
                    else
                        Screen.ShowNotification($"{ScriptName}: Value out of allowed limits for ~b~{fieldName}~w~, Min:{min}, Max:{max}");
                }
                else
                    Screen.ShowNotification($"{ScriptName}: Invalid value for ~b~{fieldName}~w~");
            }
            else if (fieldType == FieldType.Vector3Type)
            {
                var min = (fieldInfo as FieldInfo<Vector3>).Min;
                var max = (fieldInfo as FieldInfo<Vector3>).Max;

                var minValueX = min.X;
                var minValueY = min.Y;
                var minValueZ = min.Z;
                var maxValueX = max.X;
                var maxValueY = max.Y;
                var maxValueZ = max.Z;

                if (itemText.EndsWith("_x"))
                {
                    if (float.TryParse(text, out float newvalue))
                    {
                        if (newvalue >= minValueX && newvalue <= maxValueX)
                        {
                            dynamicListItem.CurrentItem = newvalue.ToString("F3");
                            // Notify the value is changed so the preset can update...
                            MenuPresetValueChanged?.Invoke(fieldName, newvalue.ToString("F3"), itemText);
                        }
                        else
                            Screen.ShowNotification($"{ScriptName}: Value out of allowed limits for ~b~{itemText}~w~, Min:{minValueX}, Max:{maxValueX}");
                    }
                    else
                        Screen.ShowNotification($"{ScriptName}: Invalid value for ~b~{itemText}~w~");
                }
                else if (itemText.EndsWith("_y"))
                {
                    if (float.TryParse(text, out float newvalue))
                    {
                        if (newvalue >= minValueY && newvalue <= maxValueY)
                        {
                            dynamicListItem.CurrentItem = newvalue.ToString("F3");
                            // Notify the value is changed so the preset can update...
                            MenuPresetValueChanged?.Invoke(fieldName, newvalue.ToString("F3"), itemText);
                        }
                        else
                            Screen.ShowNotification($"{ScriptName}: Value out of allowed limits for ~b~{itemText}~w~, Min:{minValueY}, Max:{maxValueY}");
                    }
                    else
                        Screen.ShowNotification($"{ScriptName}: Invalid value for ~b~{itemText}~w~");
                }
                else if (itemText.EndsWith("_z"))
                {
                    if (float.TryParse(text, out float newvalue))
                    {
                        if (newvalue >= minValueZ && newvalue <= maxValueZ)
                        {
                            dynamicListItem.CurrentItem = newvalue.ToString("F3");
                            // Notify the value is changed so the preset can update...
                            MenuPresetValueChanged?.Invoke(fieldName, newvalue.ToString("F3"), itemText);
                        }
                        else
                            Screen.ShowNotification($"{ScriptName}: Value out of allowed limits for ~b~{itemText}~w~, Min:{minValueZ}, Max:{maxValueZ}");
                    }
                    else
                        Screen.ShowNotification($"{ScriptName}: Invalid value for ~b~{itemText}~w~");
                }
            }
        }

        /// <summary>
        /// Invoked when the value of a dynamic list item from the main editor menu is changed
        /// </summary>
        /// <param name="menu"></param>
        /// <param name="dynamicListItem"></param>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        private void EditorMenu_OnDynamicListItemCurrentItemChange(Menu menu, MenuDynamicListItem dynamicListItem, string oldValue, string newValue)
        {
            // If the sender isn't the main editor menu...
            if (menu != EditorMenu)
                return;

            // If item data is not the expected one...
            if (!(dynamicListItem.ItemData is BaseFieldInfo fieldInfo))
                return;

            // Get field name which is controlled by this dynamic list item
            string fieldName = fieldInfo.Name;

            // Notify the value is changed so the preset can update...
            MenuPresetValueChanged?.Invoke(fieldName, newValue, dynamicListItem.Text);
        }

        /// <summary>
        /// Rebuild the main editor menu
        /// </summary>
        private void UpdateEditorMenu()
        {
            if (EditorMenu == null)
                return;

            EditorMenu.ClearMenuItems();

            // Create Personal Presets sub menu and bind item to a button
            var PersonalPresetsItem = new MenuItem("Personal Presets", "The handling presets saved by you.")
            {
                Label = "→→→"
            };
            EditorMenu.AddMenuItem(PersonalPresetsItem);
            MenuController.BindMenuItem(EditorMenu, PersonalPresetsMenu, PersonalPresetsItem);

            // Create Server Presets sub menu and bind item to a button
            var ServerPresetsItem = new MenuItem("Server Presets", "The handling presets loaded from the server.")
            {
                Label = "→→→"
            };
            EditorMenu.AddMenuItem(ServerPresetsItem);
            MenuController.BindMenuItem(EditorMenu, ServerPresetsMenu, ServerPresetsItem);

            if (!CurrentPresetIsValid)
                return;

            // Add all the controllers
            foreach (var item in HandlingInfo.FieldsInfo)
            {
                var fieldInfo = item.Value;

                if (fieldInfo.Editable)
                {

                    //string fieldName = fieldInfo.Name;
                    //string fieldDescription = fieldInfo.Description;
                    Type fieldType = fieldInfo.Type;

                    if (fieldType == FieldType.FloatType)
                        AddDynamicFloatList(EditorMenu, (FieldInfo<float>)item.Value);
                    else if (fieldType == FieldType.IntType)
                        AddDynamicIntList(EditorMenu, (FieldInfo<int>)item.Value);
                    else if (fieldType == FieldType.Vector3Type)
                        AddDynamicVector3List(EditorMenu, (FieldInfo<Vector3>)item.Value);
                }
                else
                {
                    AddLockedItem(EditorMenu, item.Value);
                }
            }

            var resetItem = new MenuItem("Reset", "Restores the default values")
            {
                ItemData = "handling_reset",
            };
            EditorMenu.AddMenuItem(resetItem);
        }

        /// <summary>
        /// Rebuild the personal presets menu
        /// </summary>
        private void UpdatePersonalPresetsMenu()
        {
            if (PersonalPresetsMenu == null)
                return;

            PersonalPresetsMenu.ClearMenuItems();

            KvpEnumerable kvpList = new KvpEnumerable(kvpPrefix);
            foreach (var key in kvpList)
            {
                string value = GetResourceKvpString(key);
                PersonalPresetsMenu.AddMenuItem(new MenuItem(key.Remove(0, kvpPrefix.Length)) { ItemData = key });
            }
        }

        /// <summary>
        /// Rebuild the server presets menu
        /// </summary>
        private void UpdateServerPresetsMenu()
        {
            if (ServerPresetsMenu == null)
                return;

            ServerPresetsMenu.ClearMenuItems();

            foreach (var preset in ServerPresets)
                ServerPresetsMenu.AddMenuItem(new MenuItem(preset.Key) { ItemData = preset.Key});
        }

        /// <summary>
        /// Get a string from the user using the on screen keyboard
        /// </summary>
        /// <param name="defaultText">The default value to display</param>
        /// <returns></returns>
        private async Task<string> GetOnScreenString(string defaultText)
        {
            //var currentMenu = MenuController.GetCurrentMenu();
            //currentMenu.Visible = false;
            //MenuController.DisableMenuButtons = true;

            //DisableAllControlActions(1);
   
            DisplayOnscreenKeyboard(1, "HANDLING_EDITOR_ENTER_VALUE", "", defaultText, "", "", "", 128);
            while (UpdateOnscreenKeyboard() != 1 && UpdateOnscreenKeyboard() != 2) await Delay(100);

            //EnableAllControlActions(1);

            //MenuController.DisableMenuButtons = false;
            //currentMenu.Visible = true;

            return GetOnscreenKeyboardResult();
        }

        /// <summary>
        /// The method that defines how a dynamic list item changes its value when you press left/right arrow
        /// </summary>
        /// <param name="item"></param>
        /// <param name="left"></param>
        /// <returns></returns>
        private string DynamicListChangeCallback(MenuDynamicListItem item, bool left)
        {
            var currentItem = item.CurrentItem;

            if (!(item.ItemData is BaseFieldInfo fieldInfo))
                return currentItem;

            var itemText = item.Text;
            var fieldName = fieldInfo.Name;
            var fieldType = fieldInfo.Type;

            if (fieldType == FieldType.IntType)
            {
                var value = int.Parse(currentItem);
                var min = (fieldInfo as FieldInfo<int>).Min;
                var max = (fieldInfo as FieldInfo<int>).Max;

                if (left)
                {
                    var newvalue = value - 1;
                    if (newvalue < min)
                        Screen.ShowNotification($"{ScriptName}: Min value allowed for ~b~{fieldName}~w~ is {min}");
                    else
                    {
                        value = newvalue;
                    }
                }
                else
                {
                    var newvalue = value + 1;
                    if (newvalue > max)
                        Screen.ShowNotification($"{ScriptName}: Max value allowed for ~b~{fieldName}~w~ is {max}");
                    else
                    {
                        value = newvalue;
                    }
                }
                return value.ToString();
            }
            else if (fieldType == FieldType.FloatType)
            {
                var value = float.Parse(currentItem);
                var min = (fieldInfo as FieldInfo<float>).Min;
                var max = (fieldInfo as FieldInfo<float>).Max;

                if (left)
                {
                    var newvalue = value - FloatStep;
                    if (newvalue < min)
                        Screen.ShowNotification($"{ScriptName}: Min value allowed for ~b~{fieldName}~w~ is {min}");
                    else
                    {
                        value = newvalue;
                    }
                }
                else
                {
                    var newvalue = value + FloatStep;
                    if (newvalue > max)
                        Screen.ShowNotification($"{ScriptName}: Max value allowed for ~b~{fieldName}~w~ is {max}");
                    else
                    {
                        value = newvalue;
                    }
                }
                return value.ToString("F3");
            }
            else if (fieldType == FieldType.Vector3Type)
            {
                var value = float.Parse(currentItem);
                var min = (fieldInfo as FieldInfo<Vector3>).Min;
                var max = (fieldInfo as FieldInfo<Vector3>).Max;

                var minValueX = min.X;
                var minValueY = min.Y;
                var minValueZ = min.Z;
                var maxValueX = max.X;
                var maxValueY = max.Y;
                var maxValueZ = max.Z;

                if (itemText.EndsWith("_x"))
                {
                    if (left)
                    {
                        var newvalue = value - FloatStep;
                        if (newvalue < minValueX)
                            Screen.ShowNotification($"{ScriptName}: Min value allowed for ~b~{itemText}~w~ is {minValueX}");
                        else
                        {
                            value = newvalue;
                        }
                    }
                    else
                    {
                        var newvalue = value + FloatStep;
                        if (newvalue > maxValueX)
                            Screen.ShowNotification($"{ScriptName}: Max value allowed for ~b~{itemText}~w~ is {maxValueX}");
                        else
                        {
                            value = newvalue;
                        }
                    }
                    return value.ToString("F3");
                }
                else if (itemText.EndsWith("_y"))
                {
                    if (left)
                    {
                        var newvalue = value - FloatStep;
                        if (newvalue < minValueY)
                            Screen.ShowNotification($"{ScriptName}: Min value allowed for ~b~{itemText}~w~ is {minValueY}");
                        else
                        {
                            value = newvalue;
                        }
                    }
                    else
                    {
                        var newvalue = value + FloatStep;
                        if (newvalue > maxValueY)
                            Screen.ShowNotification($"{ScriptName}: Max value allowed for ~b~{itemText}~w~ is {maxValueY}");
                        else
                        {
                            value = newvalue;
                        }
                    }
                    return value.ToString("F3");
                }
                else if (itemText.EndsWith("_z"))
                {
                    if (left)
                    {
                        var newvalue = value - FloatStep;
                        if (newvalue < minValueZ)
                            Screen.ShowNotification($"{ScriptName}: Min value allowed for ~b~{itemText}~w~ is {minValueZ}");
                        else
                        {
                            value = newvalue;
                        }
                    }
                    else
                    {
                        var newvalue = value + FloatStep;
                        if (newvalue > maxValueZ)
                            Screen.ShowNotification($"{ScriptName}: Max value allowed for ~b~{itemText}~w~ is {maxValueZ}");
                        else
                        {
                            value = newvalue;
                        }
                    }
                    return value.ToString("F3");
                }
            }

            return currentItem;
        }

        private MenuDynamicListItem AddDynamicFloatList(Menu menu, FieldInfo<float> fieldInfo)
        {
            string fieldName = fieldInfo.Name;
            string description = fieldInfo.Description;

            if (!CurrentPreset.Fields.TryGetValue(fieldName, out dynamic tmp))
                return null;

            var value = (float)tmp;
            var newitem = new MenuDynamicListItem(fieldName, value.ToString("F3"), DynamicListChangeCallback, description)
            {
                ItemData = fieldInfo
            };
            menu.AddMenuItem(newitem);

            return newitem;
        }

        private MenuDynamicListItem AddDynamicIntList(Menu menu, FieldInfo<int> fieldInfo)
        {
            string fieldName = fieldInfo.Name;
            string description = fieldInfo.Description;

            if (!CurrentPreset.Fields.TryGetValue(fieldName, out dynamic tmp))
                return null;

            var value = (int)tmp;
            var newitem = new MenuDynamicListItem(fieldName, value.ToString(), DynamicListChangeCallback, description)
            {
                ItemData = fieldInfo
            };
            menu.AddMenuItem(newitem);

            return newitem;
        }

        private MenuDynamicListItem[] AddDynamicVector3List(Menu menu, FieldInfo<Vector3> fieldInfo)
        {
            string fieldName = fieldInfo.Name;

            if (!CurrentPreset.Fields.TryGetValue(fieldName, out dynamic tmp))
                return null;

            var value = (Vector3)tmp;

            string fieldDescription = fieldInfo.Description;

            string fieldNameX = $"{fieldName}_x";
            string fieldNameY = $"{fieldName}_y";
            string fieldNameZ = $"{fieldName}_z";

            var newitemX = new MenuDynamicListItem(fieldNameX, value.X.ToString("F3"), DynamicListChangeCallback, fieldDescription)
            {
                ItemData = fieldInfo
            };
            menu.AddMenuItem(newitemX);

            var newitemY = new MenuDynamicListItem(fieldNameY, value.Y.ToString("F3"), DynamicListChangeCallback, fieldDescription)
            {
                ItemData = fieldInfo
            };
            menu.AddMenuItem(newitemY);

            var newitemZ = new MenuDynamicListItem(fieldNameZ, value.Z.ToString("F3"), DynamicListChangeCallback, fieldDescription)
            {
                ItemData = fieldInfo
            };
            menu.AddMenuItem(newitemZ);

            return new MenuDynamicListItem[3] { newitemX, newitemY, newitemZ };
        }

        private MenuItem AddLockedItem(Menu menu, BaseFieldInfo fieldInfo)
        {
            var newitem = new MenuItem(fieldInfo.Name, fieldInfo.Description)
            {
                Enabled = false,
                RightIcon = MenuItem.Icon.LOCK,
                ItemData = fieldInfo,
            };

            menu.AddMenuItem(newitem);
            return newitem;
        }

        #endregion
    }
}