using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TerrariaLocalizationPacker.Windows;
using IOPath = System.IO.Path;
using IOFile = System.IO.File;
using IODirectory = System.IO.Directory;
using TerrariaLocalizationPacker.Properties;
using Microsoft.Win32;
using System.Xml;
using System.Diagnostics;
using TerrariaLocalizationPacker.Packing;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;

namespace TerrariaLocalizationPacker {
	/**<summary>The main window running Terraria Item Modifier.</summary>*/
	public partial class MainWindow : Window {
		//========== CONSTANTS ===========
		#region Constants

		/**<summary>The possibly paths to the Terraria executable.</summary>*/
		private static readonly string[] PossibleTerrariaPaths = {
			@"C:\Program Files (x86)\Steam\steamapps\common\Terraria\Terraria.exe",
			@"C:\Program Files\Steam\steamapps\common\Terraria\Terraria.exe",
			@"C:\Steam\steamapps\common\Terraria\Terraria.exe"
		};

		#endregion
		//=========== MEMBERS ============
		#region Members

		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Constructs the main window.</summary>*/
		public MainWindow() {
			InitializeComponent();

			LoadSettings();
			
			// Disable drag/drop text in textboxes so you can scroll their contents easily
			DataObject.AddCopyingHandler(textBoxExe, (sender, e) => { if (e.IsDragDrop) e.CancelCommand(); });
			DataObject.AddCopyingHandler(textBoxOutput, (sender, e) => { if (e.IsDragDrop) e.CancelCommand(); });
			DataObject.AddCopyingHandler(textBoxInput, (sender, e) => { if (e.IsDragDrop) e.CancelCommand(); });
		}

		#endregion
		//=========== SETTINGS ===========
		#region Settings

		/**<summary>Loads the application settings.</summary>*/
		private void LoadSettings() {
			LocalizationPacker.ExePath = Settings.Default.ExePath;
			if (string.IsNullOrEmpty(LocalizationPacker.ExePath)) {
				LocalizationPacker.ExePath = "";
				if (!string.IsNullOrEmpty(TerrariaLocator.TerrariaPath)) {
					LocalizationPacker.ExePath = TerrariaLocator.TerrariaPath;
				}
			}
			LocalizationPacker.OutputDirectory = Settings.Default.OutputDirectory;
			if (string.IsNullOrEmpty(LocalizationPacker.OutputDirectory))
				LocalizationPacker.OutputDirectory = LocalizationPacker.AppDirectory;
			LocalizationPacker.InputDirectory = Settings.Default.InputDirectory;
			if (string.IsNullOrEmpty(LocalizationPacker.InputDirectory))
				LocalizationPacker.InputDirectory = LocalizationPacker.AppDirectory;

			textBoxExe.Text = LocalizationPacker.ExePath;
			textBoxOutput.Text = LocalizationPacker.OutputDirectory;
			textBoxInput.Text = LocalizationPacker.InputDirectory;
		}
		/**<summary>Saves the application settings.</summary>*/
		private void SaveSettings() {
			Settings.Default.ExePath = LocalizationPacker.ExePath;
			Settings.Default.Save();
		}

		#endregion
		//=========== HELPERS ============
		#region Helpers
		
		/**<summary>Checks if the path is valid.</summary>*/
		private bool ValidPathTest(bool checkExists = true) {
			if (LocalizationPacker.ExePath == "") {
				TriggerMessageBox.Show(this, MessageIcon.Warning, "The Terraria path cannot be empty!", "Invalid Path");
				return false;
			}
			try {
				if (!IOFile.Exists(LocalizationPacker.ExePath) && checkExists) {
					TriggerMessageBox.Show(this, MessageIcon.Warning, "Could not find Terraria executable!", "Missing Exe");
					return false;
				}
			}
			catch (ArgumentException) {
				TriggerMessageBox.Show(this, MessageIcon.Warning, "You must enter a valid Terraria path!", "Invalid Path");
				return false;
			}
			
			return true;
		}
		/**<summary>Checks if the path is valid.</summary>*/
		private bool ValidPathTest2(bool input) {
			string directory = input ? LocalizationPacker.InputDirectory : LocalizationPacker.OutputDirectory;
			string name = input ? "Repack" : "Unpack";
			if (directory == "") {
				TriggerMessageBox.Show(this, MessageIcon.Warning, "The " + name + " folder path cannot be empty!", "Invalid Path");
				return false;
			}
			try {
				if (!IODirectory.Exists(directory)) {
					TriggerMessageBox.Show(this, MessageIcon.Warning, "Could not find " + name + " folder!", "Invalid Path");
					return false;
				}
			}
			catch (ArgumentException) {
				TriggerMessageBox.Show(this, MessageIcon.Warning, "You must enter a valid " + name + " folder path!", "Invalid Path");
				return false;
			}

			return true;
		}

		#endregion
		//============ EVENTS ============
		#region Events
		//--------------------------------
		#region Regular

		private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e) {
			SaveSettings();
		}
		private void OnRepack(object sender, RoutedEventArgs e) {
			MessageBoxResult result;
			if (!ValidPathTest() || !ValidPathTest2(true))
				return;
			result = TriggerMessageBox.Show(this, MessageIcon.Question, "Are you sure you want to repack localizations into the current Terraria executable?", "Repack Localizations", MessageBoxButton.YesNo);
			if (result == MessageBoxResult.No)
				return;
			try {
				bool filesFound = LocalizationPacker.Repack();
				if (filesFound)
					TriggerMessageBox.Show(this, MessageIcon.Info, "Localizations successfully repacked!", "Localizations Repacked");
				else
					TriggerMessageBox.Show(this, MessageIcon.Info, "No localization files with the correct names were found in the Repack folder!", "No Localizations");
			}
			catch (Exception ex) {
				result = TriggerMessageBox.Show(this, MessageIcon.Error, "An error occurred while repacking localizations! Would you like to see the error?", "Repack Error", MessageBoxButton.YesNo);
				if (result == MessageBoxResult.Yes)
					ErrorMessageBox.Show(ex, true);
				return;
			}
		}
		private void OnUnpack(object sender, RoutedEventArgs e) {
			MessageBoxResult result = MessageBoxResult.No;
			if (!ValidPathTest() || !ValidPathTest2(false))
				return;
			try {
				LocalizationPacker.Unpack();
				result = TriggerMessageBox.Show(this, MessageIcon.Info, "Localizations successfully unpacked! Would you like to open the output folder?", "Localizations Unpacked", MessageBoxButton.YesNo);
				if (result == MessageBoxResult.Yes)
					Process.Start(LocalizationPacker.OutputDirectory);
			}
			catch (Exception ex) {
				result = TriggerMessageBox.Show(this, MessageIcon.Error, "An error occurred while unpacking localizations! Would you like to see the error?", "Unpack Error", MessageBoxButton.YesNo);
				if (result == MessageBoxResult.Yes)
					ErrorMessageBox.Show(ex, true);
				return;
			}
		}
		private void OnRestore(object sender, RoutedEventArgs e) {
			MessageBoxResult result;
			if (!ValidPathTest(false))
				return;
			result = TriggerMessageBox.Show(this, MessageIcon.Question, "Are you sure you want to restore the current Terraria executable to its backup?", "Restore Terraria", MessageBoxButton.YesNo);
			if (result == MessageBoxResult.No)
				return;
			if (!IOFile.Exists(LocalizationPacker.BackupPath)) {
				TriggerMessageBox.Show(this, MessageIcon.Warning, "Could not find Terraria backup!", "Missing Backup");
				return;
			}
			try {
				LocalizationPacker.Restore();
				TriggerMessageBox.Show(this, MessageIcon.Info, "Terraria successfully restored!", "Terraria Restored");
			}
			catch (Exception ex) {
				result = TriggerMessageBox.Show(this, MessageIcon.Error, "An error occurred while restoring Terraria! Would you like to see the error?", "Restore Error", MessageBoxButton.YesNo);
				if (result == MessageBoxResult.Yes)
					ErrorMessageBox.Show(ex, true);
			}
		}

		#endregion
		//--------------------------------
		#region Settings

		private void OnBrowseExe(object sender, RoutedEventArgs e) {
			OpenFileDialog fileDialog = new OpenFileDialog();

			fileDialog.Title = "Find Terraria Executable";
			fileDialog.AddExtension = true;
			fileDialog.DefaultExt = ".exe";
			fileDialog.Filter = "Executables (*.exe)|*.exe|All Files (*.*)|*.*";
			fileDialog.FilterIndex = 0;
			fileDialog.CheckFileExists = true;
			if (LocalizationPacker.ExePath != "")
				fileDialog.InitialDirectory = LocalizationPacker.ExeDirectory;

			var result = fileDialog.ShowDialog(this);
			if (result.HasValue && result.Value) {
				LocalizationPacker.ExePath = fileDialog.FileName;
				textBoxExe.Text = fileDialog.FileName;
				SaveSettings();
			}
		}
		private void OnBrowseOutput(object sender, RoutedEventArgs e) {
			FolderBrowserDialog browser = new FolderBrowserDialog();
			browser.Description = "Choose output folder";
			browser.SelectedPath = LocalizationPacker.OutputDirectory;
			browser.ShowNewFolderButton = true;
			var result = browser.ShowFolderBrowser(this);
			if (result.HasValue && result.Value) {
				LocalizationPacker.OutputDirectory = browser.SelectedPath;
				textBoxOutput.Text = browser.SelectedPath;
			}
			browser.Dispose();
			browser = null;
		}
		private void OnBrowseInput(object sender, RoutedEventArgs e) {
			FolderBrowserDialog browser = new FolderBrowserDialog();
			browser.Description = "Choose input folder";
			browser.SelectedPath = LocalizationPacker.InputDirectory;
			browser.ShowNewFolderButton = true;
			var result = browser.ShowFolderBrowser(this);
			if (result.HasValue && result.Value) {
				LocalizationPacker.InputDirectory = browser.SelectedPath;
				textBoxInput.Text = browser.SelectedPath;
			}
			browser.Dispose();
			browser = null;
		}

		private void OnExeChanged(object sender, TextChangedEventArgs e) {
			LocalizationPacker.ExePath = textBoxExe.Text;
		}
		private void OnOutputChanged(object sender, TextChangedEventArgs e) {
			LocalizationPacker.OutputDirectory = textBoxOutput.Text;
		}
		private void OnInputChanged(object sender, TextChangedEventArgs e) {
			LocalizationPacker.InputDirectory = textBoxInput.Text;
		}

		#endregion
		//--------------------------------
		#region Menu Items

		private void OnLaunchTerraria(object sender, RoutedEventArgs e) {
			try {
				if (IOFile.Exists(LocalizationPacker.ExePath))
					Process.Start(LocalizationPacker.ExePath);
				else
					TriggerMessageBox.Show(this, MessageIcon.Warning, "Could not locate the Terraria executable! Cannot launch Terraria.", "Missing Executable");
			}
			catch {
				TriggerMessageBox.Show(this, MessageIcon.Warning, "The current path to Terraria is invalid! Cannot launch Terraria.", "Invalid Path");
			}
		}
		private void OnOpenTerrariaFolder(object sender, RoutedEventArgs e) {
			try {
				if (IODirectory.Exists(LocalizationPacker.ExeDirectory))
					Process.Start(LocalizationPacker.ExeDirectory);
				else
					TriggerMessageBox.Show(this, MessageIcon.Warning, "Could not locate the Terraria folder! Cannot open folder.", "Missing Folder");
			}
			catch {
				TriggerMessageBox.Show(this, MessageIcon.Warning, "The current path to Terraria is invalid! Cannot open folder.", "Invalid Path");
			}
		}
		private void OnOpenOutputFolder(object sender, RoutedEventArgs e) {
			try {
				if (IODirectory.Exists(LocalizationPacker.OutputDirectory))
					Process.Start(LocalizationPacker.OutputDirectory);
				else
					TriggerMessageBox.Show(this, MessageIcon.Warning, "Could not locate the Output folder! Cannot open folder.", "Missing Folder");
			}
			catch {
				TriggerMessageBox.Show(this, MessageIcon.Warning, "The current path to Output is invalid! Cannot open folder.", "Invalid Path");
			}
		}
		private void OnOpenInputFolder(object sender, RoutedEventArgs e) {
			try {
				if (IODirectory.Exists(LocalizationPacker.InputDirectory))
					Process.Start(LocalizationPacker.InputDirectory);
				else
					TriggerMessageBox.Show(this, MessageIcon.Warning, "Could not locate the Input folder! Cannot open folder.", "Missing Folder");
			}
			catch {
				TriggerMessageBox.Show(this, MessageIcon.Warning, "The current path to Input is invalid! Cannot open folder.", "Invalid Path");
			}
		}
		private void OnExit(object sender, RoutedEventArgs e) {
			Close();
		}

		private void OnAbout(object sender, RoutedEventArgs e) {
			AboutWindow.Show(this);
		}
		private void OnHelp(object sender, RoutedEventArgs e) {
			Process.Start("https://github.com/trigger-death/TerrariaLocalizationPacker/wiki");
		}
		private void OnCredits(object sender, RoutedEventArgs e) {
			CreditsWindow.Show(this);
		}
		private void OnViewOnGitHub(object sender, RoutedEventArgs e) {
			Process.Start("https://github.com/trigger-death/TerrariaLocalizationPacker");
		}

		#endregion
		//--------------------------------
		#endregion
	}
}
