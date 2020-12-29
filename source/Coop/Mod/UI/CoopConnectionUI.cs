using TaleWorlds.Engine.Screens;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using Coop.Mod.UI;

namespace Coop.Mod
{

    public interface ICoopConnectionUI
    {

    }


    class CoopConnectionUI : ScreenBase, ICoopConnectionUI
    {
        private CoopConnectMenuVM _dataSource;
        private GauntletLayer _gauntletLayer;
        private GauntletMovie _gauntletMovie;

        public CoopConnectionUI(ICoopConnectMenuVM coopConnectMenuVM)
        {
            _dataSource = (CoopConnectMenuVM) coopConnectMenuVM; // As LoadMovie method needs a ViewModel instance, this cast is a must. TODO Check if it's really necessary
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _gauntletLayer = new GauntletLayer(100)
            {
                IsFocusLayer = true
            };
            AddLayer(_gauntletLayer);
            _gauntletLayer.InputRestrictions.SetInputRestrictions();
            _gauntletMovie = _gauntletLayer.LoadMovie("CoopConnectionUIMovie", _dataSource);
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            ScreenManager.TrySetFocus(_gauntletLayer);
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();
            _gauntletLayer.IsFocusLayer = false;
            ScreenManager.TryLoseFocus(_gauntletLayer);
        }

        protected override void OnFinalize()
        {
            base.OnFinalize();
            RemoveLayer(_gauntletLayer);
            _dataSource = null;
            _gauntletLayer = null;
        }

    }
}