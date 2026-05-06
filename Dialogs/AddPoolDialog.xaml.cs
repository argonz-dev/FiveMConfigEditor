using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace FiveMConfigEditorWPF.Dialogs
{
    public partial class AddPoolDialog : Window
    {
        public string PoolKey { get; private set; } = "";
        public int PoolValue { get; private set; } = 1000;

        private static readonly string[] KnownPoolKeys =
        {
            "AnimatedBuilding", "Building", "Dummy", "DummyObject",
            "Entity", "Ped", "Rope", "ScenarioPoint",
            "ScriptedBuilding", "Vehicle", "VehicleStruct",
            "DrawableStore", "FragmentStore", "GeometryStore",
            "TxdStore", "CutsceneObject", "CTaskTree",
            "CEventHeap", "CTaskHeap", "CNetworkDefStore"
        };

        public AddPoolDialog()
        {
            InitializeComponent();
            foreach (var key in KnownPoolKeys)
                CmbPoolKey.Items.Add(key);
            CmbPoolKey.SelectedIndex = 0;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void TxtValue_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            var key = CmbPoolKey.Text?.Trim();
            if (string.IsNullOrEmpty(key))
            {
                MessageBox.Show("Pilih atau masukkan pool key.", "Validasi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(TxtValue.Text, out int val) || val <= 0)
            {
                MessageBox.Show("Masukkan nilai angka yang valid.", "Validasi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            PoolKey = key;
            PoolValue = val;
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
