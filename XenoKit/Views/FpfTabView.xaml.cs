using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using XenoKit.Editor;
using XenoKit.Engine;
using XenoKit.Windows;
using Xv2CoreLib;
using Xv2CoreLib.BCS;
using Xv2CoreLib.CUS;
using Xv2CoreLib.EAN;
using Xv2CoreLib.ESK;
using Xv2CoreLib.FPF;
using file = Xv2CoreLib.FileManager;
using xv2 = Xv2CoreLib.Xenoverse2;

namespace XenoKit.Controls
{
    public partial class FpfTabView : UserControl
    {
        private readonly List<FpfFileItem> fpfFiles = new List<FpfFileItem>();
        private readonly List<AnimationItem> animationItems = new List<AnimationItem>();
        private readonly List<PreviewPoseItem> previewPoseItems = new List<PreviewPoseItem>
        {
            new PreviewPoseItem(0, "Intro", FpfPreviewKind.Intro),
            new PreviewPoseItem(1, "Formation", FpfPreviewKind.Formation)
        };
        private bool isLoaded;
        private bool isUpdatingControls;
        private FPF_File selectedFpfFile;
        private string selectedFpfPath;
        private EAN_File selectedEanFile;
        private EAN_Animation selectedAnimation;
        private AnimationItem selectedAnimationItem;
        private PreviewPoseItem selectedPreviewPose;

        public FpfTabView()
        {
            InitializeComponent();
        }

        public List<PreviewPoseItem> PreviewPoses => previewPoseItems;
        public List<AnimationItem> AnimationItems => animationItems;
        public PreviewPoseItem SelectedPreviewPose
        {
            get => selectedPreviewPose;
            set => selectedPreviewPose = value;
        }
        public AnimationItem SelectedAnimationItem
        {
            get => selectedAnimationItem;
            set => selectedAnimationItem = value;
        }
        public static readonly DependencyProperty FigurePositionXProperty = DependencyProperty.Register(nameof(FigurePositionX), typeof(double), typeof(FpfTabView), new PropertyMetadata(0d, OnFigurePositionChanged));
        public static readonly DependencyProperty FigurePositionYProperty = DependencyProperty.Register(nameof(FigurePositionY), typeof(double), typeof(FpfTabView), new PropertyMetadata(0d, OnFigurePositionChanged));
        public static readonly DependencyProperty FigurePositionZProperty = DependencyProperty.Register(nameof(FigurePositionZ), typeof(double), typeof(FpfTabView), new PropertyMetadata(0d, OnFigurePositionChanged));
        public double FigurePositionX
        {
            get => (double)GetValue(FigurePositionXProperty);
            set => SetValue(FigurePositionXProperty, value);
        }
        public double FigurePositionY
        {
            get => (double)GetValue(FigurePositionYProperty);
            set => SetValue(FigurePositionYProperty, value);
        }
        public double FigurePositionZ
        {
            get => (double)GetValue(FigurePositionZProperty);
            set => SetValue(FigurePositionZProperty, value);
        }
        private Actor SelectedActor => Files.Instance.SelectedItem?.character;

        private static void OnFigurePositionChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            ((FpfTabView)dependencyObject).UpdateSelectedFpfPlacement();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!isLoaded)
            {
                Files.SelectedItemChanged += Files_SelectedItemChanged;
                isLoaded = true;
            }

            LoadFpfFilesForSelectedCharacter();
            UpdateControls();
        }

        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible)
                return;

            Actor actor = SelectedActor;

            if (actor?.FpfPreviewFile == null)
                return;

            StopFpfPreview(actor);
            actor.Simulate(true, false);
            SceneManager.InvokeSeekOccurredEvent();
        }

        private void Files_SelectedItemChanged(object sender, EventArgs e)
        {
            selectedFpfFile = null;
            selectedFpfPath = null;
            selectedEanFile = null;
            selectedAnimation = null;
            selectedAnimationItem = null;
            animationItems.Clear();

            LoadFpfFilesForSelectedCharacter();
            UpdateControls();
        }

        private void FpfFileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdatingControls)
                return;

            if (fpfFileComboBox.SelectedItem is FpfFileItem fpfFile)
                LoadSelectedFpf(fpfFile);
        }

        private void PoseListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdatingControls || poseListBox.SelectedItem == null)
                return;

            selectedPreviewPose = poseListBox.SelectedItem as PreviewPoseItem;
            PreviewSelectedPose();
        }

        private void PoseListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            PreviewSelectedPose();
        }

        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            PreviewSelectedPose();
        }

        private void PreviewPoseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            PreviewSelectedPose();
        }

        private void StopPreviewButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGetActor(out Actor actor))
                return;

            StopFpfPreview(actor);
            statusText.Text = "FPF preview stopped.";
        }

        private void LoadLooseEanButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog
            {
                Filter = "EAN files (*.ean)|*.ean"
            };

            if (openFile.ShowDialog(Window.GetWindow(this)) != true)
                return;

            try
            {
                SetPoseSource(EAN_File.Load(openFile.FileName, true), Path.GetFileName(openFile.FileName));
            }
            catch (Exception ex)
            {
                Log.Add($"Could not load EAN file: {ex.Message}", LogType.Error);
            }
        }

        private void LoadCharacterPoseButton_Click(object sender, RoutedEventArgs e)
        {
            EntitySelector selector = new EntitySelector(xv2.Instance.GetCharacterList(), "Character");
            selector.SetBooleanParameter("Only Load From CPK", "Ignore loose files and load directly from CPK.");
            selector.ShowDialog();

            if (selector.SelectedItem == null)
                return;

            try
            {
                Xv2Character character = xv2.Instance.GetCharacter(selector.SelectedItem.ID, true, selector.BooleanParameter);
                EAN_File eanFile = character?.MovesetFiles?.EanFile?.FirstOrDefault()?.File;

                if (eanFile == null)
                {
                    Log.Add("The selected character does not have a loaded EAN file.", LogType.Error);
                    return;
                }

                SetPoseSource(eanFile, selector.SelectedItem.Name);
            }
            catch (Exception ex)
            {
                Log.Add($"Could not load character pose source: {ex.Message}", LogType.Error);
            }
        }

        private void LoadSkillPoseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is MenuItem item) || !Enum.TryParse(item.Tag?.ToString(), out CUS_File.SkillType skillType))
                return;

            EntitySelector selector = new EntitySelector(xv2.Instance.GetSkillList(skillType), skillType.ToString());
            selector.SetBooleanParameter("Only Load From CPK", "Ignore loose files and load directly from CPK.");
            selector.ShowDialog();

            if (selector.SelectedItem == null)
                return;

            try
            {
                Xv2Skill skill = xv2.Instance.GetSkill(skillType, selector.SelectedItem.ID, true, selector.BooleanParameter);
                EAN_File eanFile = skill?.Files?.EanFile?.FirstOrDefault()?.File;

                if (eanFile == null)
                {
                    Log.Add("The selected skill does not have a loaded EAN file.", LogType.Error);
                    return;
                }

                SetPoseSource(eanFile, selector.SelectedItem.Name);
            }
            catch (Exception ex)
            {
                Log.Add($"Could not load skill pose source: {ex.Message}", LogType.Error);
            }
        }

        private void AnimationListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdatingControls)
                return;

            if (animationListBox.SelectedItem is AnimationItem animationItem)
            {
                selectedAnimationItem = animationItem;
                selectedAnimation = animationItem.Animation;
                PlaySelectedPoseSourceAnimation();
                UpdateControls();
            }
        }

        private void AnimationListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            PlaySelectedPoseSourceAnimation();
        }

        private void PlayAnimationMenuItem_Click(object sender, RoutedEventArgs e)
        {
            PlaySelectedPoseSourceAnimation();
        }

        private void BakeButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGetActorAndFpf(out Actor actor))
                return;

            if (selectedAnimation == null)
            {
                Log.Add("Load a pose source and select an animation before baking.", LogType.Error);
                return;
            }

            StopFpfPreview(actor);
            actor.Simulate(true, false);

            FPF_File bakeFpfFile = LoadFpfFile(selectedFpfPath);
            FPF_Entry mainEntry = bakeFpfFile.GetMainSkeletonEntry();

            if (mainEntry?.BonePoses == null || mainEntry.BonePoses.Count != actor.Skeleton.Bones.Length)
            {
                Log.Add("The selected FPF main entry does not match the selected character skeleton.", LogType.Error);
                return;
            }

            actor.PartSet?.Update();

            int bakedEntries = FpfPosePreview.BakeSkeletonPoseToEntry(actor.Skeleton, mainEntry, FpfPoseBakeMode.CurrentPose);
            bakedEntries += actor.PartSet?.BakeFpfPreviewPhysicsPoses(bakeFpfFile, FpfPoseBakeMode.CurrentPose) ?? 0;

            SaveFileDialog saveFile = new SaveFileDialog
            {
                Filter = "FPF files (*.fpf)|*.fpf",
                FileName = Path.GetFileNameWithoutExtension(selectedFpfPath) + "_baked.fpf",
                InitialDirectory = GetInitialSaveDirectory()
            };

            if (saveFile.ShowDialog(Window.GetWindow(this)) != true)
                return;

            int currentFrame = actor.AnimationPlayer.PrimaryAnimation?.CurrentFrame_Int ?? 0;
            File.WriteAllBytes(saveFile.FileName, bakeFpfFile.Write().ToArray());
            statusText.Text = $"Baked {selectedAnimation.Name} frame {currentFrame} to {Path.GetFileName(saveFile.FileName)}.";
            Log.Add($"Baked FPF pose to {saveFile.FileName}. Updated {bakedEntries} FPF entries.", LogType.Info);
        }

        private void BakeCurrentFigurePoseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGetActorAndFpf(out Actor actor))
                return;

            try
            {
                PreviewSelectedPose();
                actor.FpfPreviewUsePlacementOffset = false;
                actor.Simulate(true, false);

                FPF_File bakeFpfFile = CloneSelectedFpfFile();
                FPF_Entry mainEntry = bakeFpfFile.GetMainSkeletonEntry();

                if (mainEntry?.BonePoses == null || mainEntry.BonePoses.Count != actor.Skeleton.Bones.Length)
                {
                    Log.Add("The selected FPF main entry does not match the selected character skeleton.", LogType.Error);
                    return;
                }

                int bakedEntries = FpfPosePreview.BakeSkeletonPoseToEntry(actor.Skeleton, mainEntry, FpfPoseBakeMode.CurrentPose);
                bakedEntries += actor.PartSet?.BakeFpfPreviewPhysicsPoses(bakeFpfFile, FpfPoseBakeMode.CurrentPose) ?? 0;

                SaveFileDialog saveFile = new SaveFileDialog
                {
                    Filter = "FPF files (*.fpf)|*.fpf",
                    FileName = Path.GetFileNameWithoutExtension(selectedFpfPath) + "_figure_pose_baked.fpf",
                    InitialDirectory = GetInitialSaveDirectory()
                };

                if (saveFile.ShowDialog(Window.GetWindow(this)) != true)
                {
                    actor.FpfPreviewUsePlacementOffset = true;
                    return;
                }

                string poseName = selectedPreviewPose?.Name ?? "figure";
                File.WriteAllBytes(saveFile.FileName, bakeFpfFile.Write().ToArray());
                actor.FpfPreviewUsePlacementOffset = true;
                statusText.Text = $"Baked current {poseName.ToLower()} figure pose to {Path.GetFileName(saveFile.FileName)}.";
                Log.Add($"Baked current FPF figure pose to {saveFile.FileName}. Updated {bakedEntries} FPF entries.", LogType.Info);
            }
            catch (Exception ex)
            {
                actor.FpfPreviewUsePlacementOffset = true;
                Log.Add($"Could not bake current FPF figure pose: {ex.Message}", LogType.Error);
            }
        }

        private void ReindexFpfSkeletonMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGetActorAndFpf(out Actor actor))
                return;

            try
            {
                string skeletonName = actor.CharacterData?.CmsEntry?.BcsPath;

                if (string.IsNullOrWhiteSpace(actor.ShortName) || string.IsNullOrWhiteSpace(skeletonName))
                {
                    Log.Add("The selected character does not have a valid CMS skeleton path.", LogType.Error);
                    return;
                }

                string sourceSkeletonPath = $"chara/{actor.ShortName}/{skeletonName}_000.esk";
                FPF_File sourceFpfFile = LoadCpkFpfForReindex(selectedFpfPath);
                ESK_File sourceSkeleton = LoadSourceSkeletonForFpfTool(sourceSkeletonPath);
                ESK_File targetSkeleton = actor.CharacterData?.EskFile?.File;

                if (sourceFpfFile == null || sourceSkeleton == null || targetSkeleton == null)
                {
                    Log.Add("Could not load the source FPF, source skeleton, or current character skeleton.", LogType.Error);
                    return;
                }

                StopFpfPreview(actor);
                ReindexMainEntry(sourceFpfFile, sourceSkeleton, targetSkeleton, sourceSkeletonPath);
                PartSet sourcePartSet = LoadCpkPartSetForFpfTool(actor.ShortName, skeletonName, sourceFpfFile.Costume);
                int secondaryEntryCount = RemapSecondaryEntriesForReindex(actor, sourceFpfFile, sourcePartSet);
                selectedFpfFile = sourceFpfFile;

                if (!SaveSelectedFpf("_reindexed", out string savePath))
                    return;

                statusText.Text = $"Re-indexed FPF skeleton to {Path.GetFileName(savePath)}.";
                Log.Add($"Re-indexed FPF skeleton and saved {savePath}. Remapped {secondaryEntryCount} secondary entries.", LogType.Info);
            }
            catch (Exception ex)
            {
                Log.Add($"Could not re-index FPF skeleton: {ex.Message}", LogType.Error);
            }
        }

        private void FixDeformationMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGetActorAndFpf(out Actor actor))
                return;

            try
            {
                string skeletonName = actor.CharacterData?.CmsEntry?.BcsPath;

                if (string.IsNullOrWhiteSpace(actor.ShortName) || string.IsNullOrWhiteSpace(skeletonName))
                {
                    Log.Add("The selected character does not have a valid CMS skeleton path.", LogType.Error);
                    return;
                }

                string sourceSkeletonPath = $"chara/{actor.ShortName}/{skeletonName}_000.esk";
                FPF_File sourceFpfFile = LoadCpkFpfForReindex(selectedFpfPath);
                ESK_File sourceSkeleton = LoadSourceSkeletonForFpfTool(sourceSkeletonPath);
                ESK_File targetSkeleton = actor.CharacterData?.EskFile?.File;

                if (sourceFpfFile == null || sourceSkeleton == null || targetSkeleton == null)
                {
                    Log.Add("Could not load the source FPF, source skeleton, or current character skeleton.", LogType.Error);
                    return;
                }

                StopFpfPreview(actor);
                FixMainEntryDeformation(sourceFpfFile, sourceSkeleton, targetSkeleton, sourceSkeletonPath);
                PartSet sourcePartSet = LoadCpkPartSetForFpfTool(actor.ShortName, skeletonName, sourceFpfFile.Costume);
                int secondaryEntryCount = FixSecondaryEntriesDeformation(actor, sourceFpfFile, sourcePartSet);
                selectedFpfFile = sourceFpfFile;

                if (!SaveSelectedFpf("_fixed_deformation", out string savePath))
                    return;

                statusText.Text = $"Fixed FPF deformation to {Path.GetFileName(savePath)}.";
                Log.Add($"Fixed FPF deformation and saved {savePath}. Fixed {secondaryEntryCount} secondary entries.", LogType.Info);
            }
            catch (Exception ex)
            {
                Log.Add($"Could not fix FPF deformation: {ex.Message}", LogType.Error);
            }
        }

        private void LoadFpfFilesForSelectedCharacter()
        {
            fpfFiles.Clear();
            selectedFpfFile = null;
            selectedFpfPath = null;

            Actor actor = SelectedActor;

            if (actor == null)
                return;

            string characterCode = actor.ShortName;

            if (string.IsNullOrWhiteSpace(characterCode))
                return;

            if (file.Instance.fileIO == null)
            {
                Log.Add("Game file IO is not ready, FPF files cannot be loaded.", LogType.Error);
                return;
            }

            string[] paths = GetFpfFilePaths(actor, characterCode);

            foreach (string path in paths.OrderBy(path => path))
                fpfFiles.Add(new FpfFileItem(path));

            FpfFileItem defaultFile = fpfFiles.FirstOrDefault(fpfFile => fpfFile.RelativePath.EndsWith("_0141.fpf", StringComparison.OrdinalIgnoreCase)) ?? fpfFiles.FirstOrDefault();

            if (defaultFile != null)
                LoadSelectedFpf(defaultFile);

            if (selectedPreviewPose == null)
                selectedPreviewPose = previewPoseItems.FirstOrDefault();
        }

        private void LoadSelectedFpf(FpfFileItem fpfFile)
        {
            try
            {
                selectedFpfFile = LoadFpfFile(fpfFile.RelativePath);
                selectedFpfPath = fpfFile.RelativePath;

                Actor actor = SelectedActor;

                if (actor?.FpfPreviewFile != null)
                {
                    actor.FpfPreviewFile = selectedFpfFile;
                    actor.FpfPreviewPath = selectedFpfPath;
                }

                SetPlacementPropertiesFromSelectedFpf();
                string costumeStatus = LoadFpfCostumeSet(actor, selectedFpfFile.Costume);
                statusText.Text = $"Loaded {fpfFile.DisplayName}. {costumeStatus}";
                Files.Instance.SelectedItemOrTabChanged(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                selectedFpfFile = null;
                selectedFpfPath = null;
                statusText.Text = "";
                Files.Instance.SelectedItemOrTabChanged(this, EventArgs.Empty);
                Log.Add($"Could not load FPF file: {ex.Message}", LogType.Error);
            }
        }

        private FPF_File LoadFpfFile(string fpfPath)
        {
            bool onlyLoadFromCpk = SelectedActor?.CharacterData?.OnlyLoadFromCPK == true;
            byte[] bytes = file.Instance.GetBytesFromGame(fpfPath, onlyLoadFromCpk, true);
            return FPF_File.Parse(bytes);
        }

        private FPF_File CloneSelectedFpfFile()
        {
            return FPF_File.Parse(selectedFpfFile.Write().ToArray());
        }

        private static string[] GetFpfFilePaths(Actor actor, string characterCode)
        {
            string directory = $"chara/{characterCode}";

            if (actor?.CharacterData?.OnlyLoadFromCPK == true)
            {
                if (file.Instance.fileIO.cpkReader == null)
                    return new string[0];

                return file.Instance.fileIO.cpkReader.GetFilesInDirectory($"data/{directory}")
                    .Where(path => Path.GetExtension(path).Equals(".fpf", StringComparison.OrdinalIgnoreCase))
                    .Select(RemoveDataPrefix)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }

            return file.Instance.fileIO.GetFilesInDirectory(directory, ".fpf", false)
                .Select(RemoveDataPrefix)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static string RemoveDataPrefix(string path)
        {
            string normalizedPath = NormalizeRelativeGamePath(path);

            if (string.IsNullOrWhiteSpace(normalizedPath))
                return normalizedPath;

            return normalizedPath.StartsWith("data/", StringComparison.OrdinalIgnoreCase)
                ? normalizedPath.Substring(5)
                : normalizedPath;
        }

        private string LoadFpfCostumeSet(Actor actor, int partSetId)
        {
            if (actor == null)
                return $"PartSet {partSetId} was not loaded because no character is selected.";

            bool partSetExists = actor.CharacterData?.BcsFile?.File?.PartSets?.Any(partSet => partSet.ID == partSetId) == true;

            if (!partSetExists)
            {
                Log.Add($"FPF references PartSet {partSetId}, but the selected character BCS does not contain it.", LogType.Error);
                return $"PartSet {partSetId} was not found.";
            }

            if (actor.PartSet?.ID != partSetId)
                actor.PartSet = new CharaPartSet(actor, partSetId);

            return $"Loaded PartSet {partSetId}.";
        }

        private void SetPoseSource(EAN_File eanFile, string sourceName)
        {
            selectedEanFile = eanFile;
            selectedAnimation = null;
            selectedAnimationItem = null;
            animationItems.Clear();

            if (selectedEanFile?.Animations != null)
            {
                foreach (EAN_Animation animation in selectedEanFile.Animations.OrderBy(animation => animation.IndexNumeric))
                    animationItems.Add(new AnimationItem(animation));
            }

            isUpdatingControls = true;
            animationListBox.ItemsSource = null;
            animationListBox.ItemsSource = animationItems;
            animationListBox.SelectedItem = animationItems.FirstOrDefault();
            isUpdatingControls = false;

            if (animationListBox.SelectedItem is AnimationItem animationItem)
            {
                selectedAnimationItem = animationItem;
                selectedAnimation = animationItem.Animation;
                PlaySelectedPoseSourceAnimation();
            }

            statusText.Text = $"Loaded pose source: {sourceName}.";
            UpdateControls();
        }

        private void PlaySelectedPoseSourceAnimation()
        {
            if (!TryGetActor(out Actor actor) || selectedEanFile == null || selectedAnimation == null)
                return;

            SetPoseSourcePlacementPreview(actor);
            actor.AnimationPlayer.PlayPrimaryAnimation(selectedEanFile, selectedAnimation.ID_UShort, 0, ushort.MaxValue, 1f, 0f, 0, true, 1f, false);
            actor.Simulate(true, false);
            SceneManager.InvokeSeekOccurredEvent();
        }

        private void UpdateControls()
        {
            isUpdatingControls = true;

            fpfFileComboBox.ItemsSource = null;
            fpfFileComboBox.ItemsSource = fpfFiles;
            fpfFileComboBox.SelectedItem = fpfFiles.FirstOrDefault(fpfFile => fpfFile.RelativePath == selectedFpfPath);
            animationListBox.ItemsSource = null;
            animationListBox.ItemsSource = animationItems;
            animationListBox.SelectedItem = selectedAnimationItem;
            poseListBox.ItemsSource = null;
            poseListBox.ItemsSource = previewPoseItems;
            poseListBox.SelectedItem = selectedPreviewPose;
            FigurePositionX = selectedFpfFile?.FigurePositionX ?? 0d;
            FigurePositionY = selectedFpfFile?.FigurePositionY ?? 0d;
            FigurePositionZ = selectedFpfFile?.FigurePositionZ ?? 0d;

            isUpdatingControls = false;
        }

        private void SetPlacementPropertiesFromSelectedFpf()
        {
            isUpdatingControls = true;
            FigurePositionX = selectedFpfFile?.FigurePositionX ?? 0d;
            FigurePositionY = selectedFpfFile?.FigurePositionY ?? 0d;
            FigurePositionZ = selectedFpfFile?.FigurePositionZ ?? 0d;
            isUpdatingControls = false;
        }

        private void UpdateSelectedFpfPlacement()
        {
            if (isUpdatingControls || selectedFpfFile == null)
                return;

            selectedFpfFile.FigurePositionX = (float)FigurePositionX;
            selectedFpfFile.FigurePositionY = (float)FigurePositionY;
            selectedFpfFile.FigurePositionZ = (float)FigurePositionZ;

            Actor actor = SelectedActor;

            if (actor?.FpfPreviewFile != null)
            {
                actor.FpfPreviewFile = selectedFpfFile;

                if (IsPoseSourcePlacementPreview(actor))
                {
                    actor.Simulate(true, false);
                    SceneManager.InvokeSeekOccurredEvent();
                }
                else
                {
                    PreviewSelectedPose();
                }
            }
        }

        private void SetPoseSourcePlacementPreview(Actor actor)
        {
            actor.FpfPreviewFile = selectedFpfFile;
            actor.FpfPreviewPath = selectedFpfPath;
            actor.FpfPreviewPoseMatrix = FpfPoseMatrix.None;
            actor.FpfPreviewSkinOffsetMatrix = FpfPoseMatrix.None;
            actor.FpfPreviewSkinOffsetMode = FpfSkinOffsetMode.InverseBindOffsetPose;
            actor.FpfPreviewUsePlacementOffset = true;
        }

        private static bool IsPoseSourcePlacementPreview(Actor actor)
        {
            return actor.FpfPreviewPoseMatrix == FpfPoseMatrix.None && actor.FpfPreviewSkinOffsetMatrix == FpfPoseMatrix.None;
        }

        private void PreviewSelectedPose()
        {
            if (!TryGetActorAndFpf(out Actor actor))
                return;

            if (poseListBox.SelectedItem is PreviewPoseItem previewPose)
                selectedPreviewPose = previewPose;

            if (selectedPreviewPose == null)
                selectedPreviewPose = previewPoseItems.FirstOrDefault();

            actor.FpfPreviewFile = selectedFpfFile;
            actor.FpfPreviewPath = selectedFpfPath;
            actor.FpfPreviewPoseMatrix = FpfPoseMatrix.AttachmentPoseTransform;
            actor.FpfPreviewUsePlacementOffset = true;

            switch (selectedPreviewPose.Kind)
            {
                case FpfPreviewKind.Intro:
                    actor.FpfPreviewSkinOffsetMatrix = FpfPoseMatrix.None;
                    actor.FpfPreviewSkinOffsetMode = FpfSkinOffsetMode.InverseBindOffsetPose;
                    break;
                case FpfPreviewKind.Formation:
                    actor.FpfPreviewSkinOffsetMatrix = FpfPoseMatrix.FormationSkinningTransform;
                    actor.FpfPreviewSkinOffsetMode = FpfSkinOffsetMode.FpfFormationSkinningTransform;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            statusText.Text = $"Previewing {selectedPreviewPose.Name.ToLower()} pose.";
        }

        private bool TryGetActor(out Actor actor)
        {
            actor = SelectedActor;

            if (actor != null)
                return true;

            Log.Add("Select a loaded character before using FPF preview.", LogType.Error);
            return false;
        }

        private bool TryGetActorAndFpf(out Actor actor)
        {
            if (!TryGetActor(out actor))
                return false;

            if (selectedFpfFile != null)
                return true;

            Log.Add("No FPF file was found for the selected character.", LogType.Error);
            return false;
        }

        private void StopFpfPreview(Actor actor)
        {
            actor.FpfPreviewFile = null;
            actor.FpfPreviewPath = null;
            actor.FpfPreviewUsePlacementOffset = true;
        }

        private string GetInitialSaveDirectory()
        {
            if (string.IsNullOrWhiteSpace(selectedFpfPath))
                return null;

            string absolutePath = file.Instance.GetAbsolutePath(selectedFpfPath);
            string directory = Path.GetDirectoryName(absolutePath);

            return Directory.Exists(directory) ? directory : null;
        }

        public string GetSaveContextFileName()
        {
            return selectedFpfFile != null && !string.IsNullOrWhiteSpace(selectedFpfPath)
                ? Path.GetFileName(selectedFpfPath)
                : null;
        }

        public bool CanSaveContextFile()
        {
            return GetSaveContextFileName() != null;
        }

        public bool SaveContextFile()
        {
            if (selectedFpfFile == null || string.IsNullOrWhiteSpace(selectedFpfPath))
            {
                Log.Add("No FPF file is selected.", LogType.Error);
                return false;
            }

            try
            {
                string savePath = file.Instance.GetAbsolutePath(selectedFpfPath);
                string directory = Path.GetDirectoryName(savePath);

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllBytes(savePath, selectedFpfFile.Write().ToArray());
                statusText.Text = $"Saved {Path.GetFileName(savePath)}.";
                Log.Add($"Saved FPF file to {savePath}.", LogType.Info);
                return true;
            }
            catch (Exception ex)
            {
                Log.Add($"Could not save FPF file: {ex.Message}", LogType.Error);
                return false;
            }
        }

        private static FPF_File LoadCpkFpfForReindex(string fpfPath)
        {
            byte[] bytes = file.Instance.GetBytesFromGame(fpfPath, true, true);
            return FPF_File.Parse(bytes);
        }

        private static ESK_File LoadSourceSkeletonForFpfTool(string sourceSkeletonPath)
        {
            return file.Instance.GetParsedFileFromGame(sourceSkeletonPath, false, true, true) as ESK_File;
        }

        private static ESK_File LoadCpkSkeletonForFpfTool(string sourceSkeletonPath)
        {
            return file.Instance.GetParsedFileFromGame(sourceSkeletonPath, true, false, true) as ESK_File;
        }

        private static PartSet LoadCpkPartSetForFpfTool(string characterCode, string bcsName, int partSetId)
        {
            string bcsPath = $"chara/{characterCode}/{bcsName}.bcs";
            BCS_File bcsFile = file.Instance.GetParsedFileFromGame(bcsPath, true, false, true) as BCS_File;
            return bcsFile?.PartSets?.FirstOrDefault(partSet => partSet.ID == partSetId);
        }

        private static void ReindexMainEntry(FPF_File fpfFile, ESK_File sourceSkeleton, ESK_File targetSkeleton, string sourceSkeletonPath)
        {
            FPF_Entry mainEntry = fpfFile.GetMainSkeletonEntry();

            if (mainEntry == null)
                throw new InvalidDataException("FPF file does not contain the main skeleton entry.");

            IList<ESK_Bone> sourceBones = GetFpfSourceBones(mainEntry, sourceSkeleton, sourceSkeletonPath, "Main FPF entry", true);
            IList<ESK_Bone> targetBones = targetSkeleton.Skeleton.NonRecursiveBones;
            mainEntry.BonePoses = RemapFpfEntry(mainEntry, sourceBones, targetBones);

            Dictionary<string, int> targetBoneIndexes = new Dictionary<string, int>();

            for (int boneIndex = 0; boneIndex < targetBones.Count; boneIndex++)
                targetBoneIndexes.Add(targetBones[boneIndex].Name, boneIndex);

            AddRootBoneAlias(sourceBones, targetBones, targetBoneIndexes);
            fpfFile.BoneIndexes.Remap(sourceBones.Select(bone => bone.Name).ToList(), targetBoneIndexes);
            fpfFile.ValidateMainSkeleton(targetBones.Select(bone => bone.Name).ToList());
        }

        private int RemapSecondaryEntriesForReindex(Actor actor, FPF_File fpfFile, PartSet sourcePartSet)
        {
            if (actor?.CharacterData?.BcsFile?.File == null || fpfFile?.Entries == null)
                return 0;

            PartSet targetPartSet = actor.CharacterData.BcsFile.File.PartSets.FirstOrDefault(partSetEntry => partSetEntry.ID == fpfFile.Costume);

            if (targetPartSet == null)
                throw new InvalidDataException($"PartSet {fpfFile.Costume} was not found in the selected character BCS.");

            List<SecondarySkeletonMatch> sourceSkeletons = sourcePartSet != null ? FindSecondarySkeletonsForReindex(sourcePartSet, true) : new List<SecondarySkeletonMatch>();
            List<SecondarySkeletonMatch> targetSkeletons = FindSecondarySkeletonsForReindex(targetPartSet, false);
            List<FPF_Entry> secondaryEntries = fpfFile.Entries
                .Where(entry => entry.ID != FPF_File.MainSkeletonEntryId)
                .OrderBy(entry => entry.ID)
                .ToList();
            IList<ESK_Bone> targetMainBones = actor.CharacterData.EskFile.File.Skeleton.NonRecursiveBones;
            int remappedCount = 0;
            List<FPF_Entry> remappedEntries = new List<FPF_Entry>
            {
                fpfFile.GetMainSkeletonEntry()
            };
            Dictionary<string, Queue<SourceSecondaryEntry>> sourceEntryQueuesByKey = GetSourceSecondaryEntryQueuesByKey(sourceSkeletons, secondaryEntries);
            Dictionary<string, Queue<SourceSecondaryEntry>> sourceEntryQueuesByPath = GetSourceSecondaryEntryQueuesByPath(sourceSkeletons, secondaryEntries);
            HashSet<int> usedEntryIds = new HashSet<int>(fpfFile.Entries.Select(entry => entry.ID));
            HashSet<int> usedSourceEntryIds = new HashSet<int>();

            foreach (SecondarySkeletonMatch targetSkeleton in targetSkeletons)
            {
                foreach (int targetEntryId in GetExpectedSecondaryEntryIds(targetSkeleton))
                {
                    FPF_Entry remappedEntry = null;

                    if (TryGetSourceSecondaryEntry(targetSkeleton, targetEntryId, sourceEntryQueuesByKey, sourceEntryQueuesByPath, usedSourceEntryIds, out SourceSecondaryEntry sourceEntry))
                    {
                        if (TryGetFpfSourceBones(sourceEntry.Entry, sourceEntry.Skeleton.SourceSkeleton, sourceEntry.Skeleton.RelativePath, false, out IList<ESK_Bone> sourceBones))
                        {
                            remappedEntry = CopyFpfEntry(sourceEntry.Entry, sourceEntry.Entry.ID);
                            remappedEntry.BonePoses = RemapFpfEntry(sourceEntry.Entry, sourceBones, targetSkeleton.TargetSkeleton.Skeleton.NonRecursiveBones);
                        }
                    }

                    if (remappedEntry == null)
                        remappedEntry = CreateBindPoseEntry(GetFpfEntryId(targetEntryId, usedEntryIds), GetSecondaryEntryTypeTemplate(secondaryEntries), targetSkeleton.TargetSkeleton.Skeleton.NonRecursiveBones, targetSkeleton.AttachBone, fpfFile.GetMainSkeletonEntry(), targetMainBones);

                    remappedEntries.Add(remappedEntry);
                    usedEntryIds.Add(remappedEntry.ID);
                    remappedCount++;
                }
            }

            fpfFile.Entries = remappedEntries;
            return remappedCount;
        }

        private int FixSecondaryEntriesDeformation(Actor actor, FPF_File fpfFile, PartSet sourcePartSet)
        {
            if (actor?.CharacterData?.BcsFile?.File == null || fpfFile?.Entries == null)
                return 0;

            PartSet targetPartSet = actor.CharacterData.BcsFile.File.PartSets.FirstOrDefault(partSetEntry => partSetEntry.ID == fpfFile.Costume);

            if (targetPartSet == null)
                throw new InvalidDataException($"PartSet {fpfFile.Costume} was not found in the selected character BCS.");

            List<SecondarySkeletonMatch> sourceSkeletons = sourcePartSet != null ? FindSecondarySkeletonsForReindex(sourcePartSet, true) : new List<SecondarySkeletonMatch>();
            List<SecondarySkeletonMatch> targetSkeletons = FindSecondarySkeletonsForReindex(targetPartSet, false);
            List<FPF_Entry> secondaryEntries = fpfFile.Entries
                .Where(entry => entry.ID != FPF_File.MainSkeletonEntryId)
                .OrderBy(entry => entry.ID)
                .ToList();
            IList<ESK_Bone> targetMainBones = actor.CharacterData.EskFile.File.Skeleton.NonRecursiveBones;
            int fixedCount = 0;
            List<FPF_Entry> fixedEntries = new List<FPF_Entry>
            {
                fpfFile.GetMainSkeletonEntry()
            };
            Dictionary<string, Queue<SourceSecondaryEntry>> sourceEntryQueuesByKey = GetSourceSecondaryEntryQueuesByKey(sourceSkeletons, secondaryEntries);
            Dictionary<string, Queue<SourceSecondaryEntry>> sourceEntryQueuesByPath = GetSourceSecondaryEntryQueuesByPath(sourceSkeletons, secondaryEntries);
            HashSet<int> usedEntryIds = new HashSet<int>(fpfFile.Entries.Select(entry => entry.ID));
            HashSet<int> usedSourceEntryIds = new HashSet<int>();

            foreach (SecondarySkeletonMatch targetSkeleton in targetSkeletons)
            {
                foreach (int targetEntryId in GetExpectedSecondaryEntryIds(targetSkeleton))
                {
                    FPF_Entry fixedEntry = null;

                    if (TryGetSourceSecondaryEntry(targetSkeleton, targetEntryId, sourceEntryQueuesByKey, sourceEntryQueuesByPath, usedSourceEntryIds, out SourceSecondaryEntry sourceEntry))
                    {
                        if (TryGetFpfSourceBones(sourceEntry.Entry, sourceEntry.Skeleton.SourceSkeleton, sourceEntry.Skeleton.RelativePath, false, out IList<ESK_Bone> sourceBones))
                        {
                            fixedEntry = CopyFpfEntry(sourceEntry.Entry, sourceEntry.Entry.ID);
                            fixedEntry.BonePoses = FixFpfEntryDeformation(sourceEntry.Entry, sourceBones, targetSkeleton.TargetSkeleton.Skeleton.NonRecursiveBones);
                        }
                    }

                    if (fixedEntry == null)
                        fixedEntry = CreateBindPoseEntry(GetFpfEntryId(targetEntryId, usedEntryIds), GetSecondaryEntryTypeTemplate(secondaryEntries), targetSkeleton.TargetSkeleton.Skeleton.NonRecursiveBones, targetSkeleton.AttachBone, fpfFile.GetMainSkeletonEntry(), targetMainBones);

                    fixedEntries.Add(fixedEntry);
                    usedEntryIds.Add(fixedEntry.ID);
                    fixedCount++;
                }
            }

            fpfFile.Entries = fixedEntries;
            return fixedCount;
        }

        private static List<SecondarySkeletonMatch> FindSecondarySkeletonsForReindex(PartSet partSet, bool onlyFromCpk)
        {
            List<SecondarySkeletonMatch> matches = new List<SecondarySkeletonMatch>();

            foreach (Part part in partSet.Parts.Where(part => part != null))
            {
                foreach (PhysicsPart physicsPart in part.PhysicsParts.Where(physicsPart => physicsPart != null))
                {
                    string relativeEskPath = NormalizeRelativeGamePath(physicsPart.GetEskPath());
                    string attachBone = physicsPart.BoneToAttach ?? "";

                    if (string.IsNullOrWhiteSpace(relativeEskPath))
                        continue;

                    ESK_File sourceSkeleton = onlyFromCpk ? LoadCpkSkeletonForFpfTool(relativeEskPath) : LoadSourceSkeletonForFpfTool(relativeEskPath);
                    ESK_File targetSkeleton = file.Instance.GetParsedFileFromGame(relativeEskPath, onlyFromCpk, false, true) as ESK_File;

                    if (sourceSkeleton != null && targetSkeleton != null)
                        matches.Add(new SecondarySkeletonMatch(relativeEskPath, attachBone, (int)part.PartType, sourceSkeleton, targetSkeleton));
                }
            }

            return matches;
        }

        private static List<FPF_BonePose> RemapFpfEntry(FPF_Entry entry, IList<ESK_Bone> sourceBones, IList<ESK_Bone> targetBones)
        {
            Dictionary<string, FPF_BonePose> sourceEntries = new Dictionary<string, FPF_BonePose>();
            List<FPF_BonePose> remappedEntries = new List<FPF_BonePose>();
            Matrix4x4[] targetBindAbsoluteMatrices = GetAbsoluteBindMatrices(targetBones);

            for (int boneIndex = 0; boneIndex < sourceBones.Count; boneIndex++)
                sourceEntries.Add(sourceBones[boneIndex].Name, entry.BonePoses[boneIndex]);

            for (int boneIndex = 0; boneIndex < targetBones.Count; boneIndex++)
            {
                if (sourceEntries.TryGetValue(targetBones[boneIndex].Name, out FPF_BonePose sourceEntry))
                    remappedEntries.Add(sourceEntry.Copy(boneIndex));
                else
                    remappedEntries.Add(CreateNewBonePoseFromRest(targetBones[boneIndex], remappedEntries, targetBindAbsoluteMatrices[boneIndex]));
            }

            return remappedEntries;
        }

        private static FPF_BonePose CreateNewBonePoseFromRest(ESK_Bone bone, IList<FPF_BonePose> targetEntries, Matrix4x4 bindAbsoluteMatrix)
        {
            Matrix4x4 relativeMatrix = GetRelativeMatrix(bone);
            Matrix4x4 parentPoseMatrix = bone.Index1 >= 0 && bone.Index1 < targetEntries.Count
                ? ToMatrix(targetEntries[bone.Index1].AttachmentPoseTransform)
                : Matrix4x4.Identity;
            Matrix4x4 absolutePoseMatrix = relativeMatrix * parentPoseMatrix;
            Matrix4x4 inverseBindMatrix = Invert(bindAbsoluteMatrix);

            return CreateFixedBonePose(targetEntries.Count, relativeMatrix, relativeMatrix, absolutePoseMatrix, inverseBindMatrix);
        }

        private static Dictionary<string, Queue<SourceSecondaryEntry>> GetSourceSecondaryEntryQueuesByKey(List<SecondarySkeletonMatch> sourceSkeletons, List<FPF_Entry> secondaryEntries)
        {
            Dictionary<string, Queue<SourceSecondaryEntry>> entriesByKey = new Dictionary<string, Queue<SourceSecondaryEntry>>(StringComparer.OrdinalIgnoreCase);
            foreach (SecondarySkeletonMatch sourceSkeleton in sourceSkeletons)
            {
                string key = GetSecondarySkeletonKey(sourceSkeleton);

                if (!entriesByKey.TryGetValue(key, out Queue<SourceSecondaryEntry> sourceEntries))
                {
                    sourceEntries = new Queue<SourceSecondaryEntry>();
                    entriesByKey.Add(key, sourceEntries);
                }

                foreach (FPF_Entry entry in GetExpectedSecondaryEntries(sourceSkeleton, secondaryEntries))
                    sourceEntries.Enqueue(new SourceSecondaryEntry(sourceSkeleton, entry));
            }

            return entriesByKey;
        }

        private static Dictionary<string, Queue<SourceSecondaryEntry>> GetSourceSecondaryEntryQueuesByPath(List<SecondarySkeletonMatch> sourceSkeletons, List<FPF_Entry> secondaryEntries)
        {
            Dictionary<string, Queue<SourceSecondaryEntry>> entriesByPath = new Dictionary<string, Queue<SourceSecondaryEntry>>(StringComparer.OrdinalIgnoreCase);
            foreach (SecondarySkeletonMatch sourceSkeleton in sourceSkeletons)
            {
                if (!entriesByPath.TryGetValue(sourceSkeleton.RelativePath, out Queue<SourceSecondaryEntry> sourceEntries))
                {
                    sourceEntries = new Queue<SourceSecondaryEntry>();
                    entriesByPath.Add(sourceSkeleton.RelativePath, sourceEntries);
                }

                foreach (FPF_Entry entry in GetExpectedSecondaryEntries(sourceSkeleton, secondaryEntries))
                    sourceEntries.Enqueue(new SourceSecondaryEntry(sourceSkeleton, entry));
            }

            return entriesByPath;
        }

        private static bool TryGetSourceSecondaryEntry(SecondarySkeletonMatch targetSkeleton, int targetEntryId, Dictionary<string, Queue<SourceSecondaryEntry>> sourceEntriesByKey, Dictionary<string, Queue<SourceSecondaryEntry>> sourceEntriesByPath, HashSet<int> usedSourceEntryIds, out SourceSecondaryEntry sourceEntry)
        {
            if (TryDequeueSourceSecondaryEntry(sourceEntriesByKey, GetSecondarySkeletonKey(targetSkeleton), targetSkeleton, targetEntryId, usedSourceEntryIds, out sourceEntry))
                return true;

            if (TryDequeueSourceSecondaryEntry(sourceEntriesByPath, targetSkeleton.RelativePath, targetSkeleton, targetEntryId, usedSourceEntryIds, out sourceEntry))
                return true;

            sourceEntry = null;
            return false;
        }

        private static bool TryDequeueSourceSecondaryEntry(Dictionary<string, Queue<SourceSecondaryEntry>> sourceEntriesByKey, string key, SecondarySkeletonMatch targetSkeleton, int targetEntryId, HashSet<int> usedSourceEntryIds, out SourceSecondaryEntry sourceEntry)
        {
            if (sourceEntriesByKey.TryGetValue(key, out Queue<SourceSecondaryEntry> sourceEntries))
            {
                while (sourceEntries.Count > 0)
                {
                    SourceSecondaryEntry nextEntry = sourceEntries.Dequeue();

                    if (usedSourceEntryIds.Contains(nextEntry.Entry.ID))
                        continue;

                    if (nextEntry.Entry.ID == targetEntryId && nextEntry.Entry?.BonePoses?.Count == targetSkeleton.TargetSkeleton?.Skeleton?.NonRecursiveBones?.Count)
                    {
                        sourceEntry = nextEntry;
                        usedSourceEntryIds.Add(nextEntry.Entry.ID);
                        return true;
                    }
                }
            }

            sourceEntry = null;
            return false;
        }

        private static IEnumerable<int> GetExpectedSecondaryEntryIds(SecondarySkeletonMatch secondarySkeleton)
        {
            int primaryEntryId = 1 + secondarySkeleton.PartType * 3;
            int formationEntryId = primaryEntryId + 34;

            if (primaryEntryId > FPF_File.MainSkeletonEntryId && primaryEntryId < FPF_File.EntryPointerListEntryCount)
                yield return primaryEntryId;

            if (formationEntryId > FPF_File.MainSkeletonEntryId && formationEntryId < FPF_File.EntryPointerListEntryCount)
                yield return formationEntryId;
        }

        private static IEnumerable<FPF_Entry> GetExpectedSecondaryEntries(SecondarySkeletonMatch secondarySkeleton, List<FPF_Entry> secondaryEntries)
        {
            HashSet<int> expectedEntryIds = new HashSet<int>(GetExpectedSecondaryEntryIds(secondarySkeleton));

            return secondaryEntries
                .Where(entry => expectedEntryIds.Contains(entry.ID))
                .Where(entry => entry.BonePoses?.Count == secondarySkeleton.SourceSkeleton?.Skeleton?.NonRecursiveBones?.Count)
                .OrderBy(entry => entry.ID);
        }

        private static int GetFpfEntryId(int preferredEntryId, HashSet<int> usedEntryIds)
        {
            if (!usedEntryIds.Contains(preferredEntryId))
                return preferredEntryId;

            for (int entryId = 1; entryId < FPF_File.EntryPointerListEntryCount; entryId++)
            {
                if (!usedEntryIds.Contains(entryId))
                    return entryId;
            }

            throw new InvalidDataException("No free FPF entry IDs are available.");
        }

        private static string GetSecondarySkeletonKey(SecondarySkeletonMatch secondarySkeleton)
        {
            return $"{secondarySkeleton.RelativePath}|{secondarySkeleton.AttachBone}";
        }

        private static int GetSecondaryEntryTypeTemplate(List<FPF_Entry> secondaryEntries)
        {
            return secondaryEntries.FirstOrDefault()?.EntryType ?? 0;
        }

        private static FPF_Entry CopyFpfEntry(FPF_Entry entry, int id)
        {
            return new FPF_Entry
            {
                ID = id,
                EntryType = entry.EntryType,
                BonePoses = entry.BonePoses?.Select(bonePose => bonePose.Copy(bonePose.Index)).ToList()
            };
        }

        private static FPF_Entry CreateBindPoseEntry(int id, int entryType, IList<ESK_Bone> bones, string attachBone = null, FPF_Entry mainEntry = null, IList<ESK_Bone> mainBones = null)
        {
            Matrix4x4[] bindAbsoluteMatrices = GetAbsoluteBindMatrices(bones);
            List<FPF_BonePose> bonePoses = new List<FPF_BonePose>();
            Dictionary<string, FPF_BonePose> mainPosesByName = GetMainPosesByName(mainEntry, mainBones);
            Matrix4x4 inverseAttachPoseMatrix = Invert(GetMainPoseMatrix(mainPosesByName, attachBone));
            Matrix4x4 inverseAttachBindMatrix = Invert(GetBindMatrix(bones, bindAbsoluteMatrices, attachBone));
            Matrix4x4[] poseAbsoluteMatrices = new Matrix4x4[bones.Count];

            for (int boneIndex = 0; boneIndex < bones.Count; boneIndex++)
            {
                ESK_Bone bone = bones[boneIndex];
                Matrix4x4 relativeMatrix = GetRelativeMatrix(bone);
                Matrix4x4 absoluteMatrix = mainPosesByName.TryGetValue(bone.Name, out FPF_BonePose mainPose)
                    ? ToMatrix(mainPose.AttachmentPoseTransform) * inverseAttachPoseMatrix
                    : bindAbsoluteMatrices[boneIndex] * inverseAttachBindMatrix;
                Matrix4x4 parentPoseMatrix = GetParentMatrix(poseAbsoluteMatrices, bone.Index1, boneIndex);
                Matrix4x4 localPoseMatrix = absoluteMatrix * Invert(parentPoseMatrix);
                Matrix4x4 inverseBindMatrix = Invert(bindAbsoluteMatrices[boneIndex]);

                poseAbsoluteMatrices[boneIndex] = absoluteMatrix;
                bonePoses.Add(CreateFixedBonePose(boneIndex, relativeMatrix, localPoseMatrix, absoluteMatrix, inverseBindMatrix));
            }

            return new FPF_Entry
            {
                ID = id,
                EntryType = entryType,
                BonePoses = bonePoses
            };
        }

        private static Dictionary<string, FPF_BonePose> GetMainPosesByName(FPF_Entry mainEntry, IList<ESK_Bone> mainBones)
        {
            Dictionary<string, FPF_BonePose> mainPosesByName = new Dictionary<string, FPF_BonePose>(StringComparer.OrdinalIgnoreCase);

            if (mainEntry?.BonePoses == null || mainBones == null)
                return mainPosesByName;

            int count = Math.Min(mainEntry.BonePoses.Count, mainBones.Count);

            for (int boneIndex = 0; boneIndex < count; boneIndex++)
            {
                if (!mainPosesByName.ContainsKey(mainBones[boneIndex].Name))
                    mainPosesByName.Add(mainBones[boneIndex].Name, mainEntry.BonePoses[boneIndex]);
            }

            return mainPosesByName;
        }

        private static Matrix4x4 GetMainPoseMatrix(Dictionary<string, FPF_BonePose> mainPosesByName, string boneName)
        {
            if (string.IsNullOrWhiteSpace(boneName) || !mainPosesByName.TryGetValue(boneName, out FPF_BonePose bonePose))
                return Matrix4x4.Identity;

            return ToMatrix(bonePose.AttachmentPoseTransform);
        }

        private static Matrix4x4 GetBindMatrix(IList<ESK_Bone> bones, Matrix4x4[] bindAbsoluteMatrices, string boneName)
        {
            int boneIndex = GetBoneIndex(bones, boneName);

            return boneIndex >= 0 ? bindAbsoluteMatrices[boneIndex] : Matrix4x4.Identity;
        }

        private static int GetBoneIndex(IList<ESK_Bone> bones, string boneName)
        {
            if (string.IsNullOrWhiteSpace(boneName))
                return -1;

            for (int boneIndex = 0; boneIndex < bones.Count; boneIndex++)
            {
                if (string.Equals(bones[boneIndex].Name, boneName, StringComparison.OrdinalIgnoreCase))
                    return boneIndex;
            }

            return -1;
        }

        private static IList<ESK_Bone> GetFpfSourceBones(FPF_Entry entry, ESK_File sourceSkeleton, string sourceSkeletonPath, string entryName, bool allowLoosePrefixFallback)
        {
            if (TryGetFpfSourceBones(entry, sourceSkeleton, sourceSkeletonPath, allowLoosePrefixFallback, out IList<ESK_Bone> sourceBones))
                return sourceBones;

            int bonePoseCount = entry?.BonePoses?.Count ?? 0;
            int sourceBoneCount = sourceSkeleton?.Skeleton?.NonRecursiveBones?.Count ?? 0;
            int cpkBoneCount = LoadCpkSkeletonForFpfTool(sourceSkeletonPath)?.Skeleton?.NonRecursiveBones?.Count ?? 0;

            throw new InvalidDataException($"{entryName} has {bonePoseCount} bone transforms, but neither the loose source skeleton ({sourceBoneCount}) nor CPK source skeleton ({cpkBoneCount}) match.");
        }

        private static bool TryGetFpfSourceBones(FPF_Entry entry, ESK_File sourceSkeleton, string sourceSkeletonPath, bool allowLoosePrefixFallback, out IList<ESK_Bone> sourceBones)
        {
            sourceBones = null;

            int bonePoseCount = entry?.BonePoses?.Count ?? 0;
            IList<ESK_Bone> looseSourceBones = sourceSkeleton?.Skeleton?.NonRecursiveBones;

            if (looseSourceBones != null && looseSourceBones.Count == bonePoseCount)
            {
                sourceBones = looseSourceBones;
                return true;
            }

            IList<ESK_Bone> cpkSourceBones = LoadCpkSkeletonForFpfTool(sourceSkeletonPath)?.Skeleton?.NonRecursiveBones;

            if (cpkSourceBones != null && cpkSourceBones.Count == bonePoseCount)
            {
                sourceBones = cpkSourceBones;
                return true;
            }

            if (looseSourceBones != null && looseSourceBones.Count > bonePoseCount && cpkSourceBones == null)
            {
                sourceBones = GetSourceBonesFromFpfRelativeTransforms(entry, looseSourceBones);

                if (sourceBones != null)
                    return true;
            }

            if (allowLoosePrefixFallback && looseSourceBones != null && looseSourceBones.Count > bonePoseCount && cpkSourceBones == null)
            {
                sourceBones = looseSourceBones.Take(bonePoseCount).ToList();
                return true;
            }

            return false;
        }

        private static IList<ESK_Bone> GetSourceBonesFromFpfRelativeTransforms(FPF_Entry entry, IList<ESK_Bone> sourceBones)
        {
            if (entry?.BonePoses == null || sourceBones == null || sourceBones.Count < entry.BonePoses.Count)
                return null;

            List<ESK_Bone> matchedBones = new List<ESK_Bone>();
            HashSet<int> usedBoneIndexes = new HashSet<int>();

            for (int poseIndex = 0; poseIndex < entry.BonePoses.Count; poseIndex++)
            {
                Matrix4x4 poseRelativeMatrix = ToMatrix(entry.BonePoses[poseIndex].RelativeTransform);
                int bestBoneIndex = -1;
                float bestDistance = float.MaxValue;

                for (int boneIndex = 0; boneIndex < sourceBones.Count; boneIndex++)
                {
                    if (usedBoneIndexes.Contains(boneIndex))
                        continue;

                    float distance = GetMatrixDistance(poseRelativeMatrix, GetRelativeMatrix(sourceBones[boneIndex]));

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestBoneIndex = boneIndex;
                    }
                }

                if (bestBoneIndex < 0 || bestDistance > 0.001f)
                    return null;

                usedBoneIndexes.Add(bestBoneIndex);
                matchedBones.Add(sourceBones[bestBoneIndex]);
            }

            return matchedBones;
        }

        private static float GetMatrixDistance(Matrix4x4 firstMatrix, Matrix4x4 secondMatrix)
        {
            float distance = 0f;

            distance += Math.Abs(firstMatrix.M11 - secondMatrix.M11);
            distance += Math.Abs(firstMatrix.M12 - secondMatrix.M12);
            distance += Math.Abs(firstMatrix.M13 - secondMatrix.M13);
            distance += Math.Abs(firstMatrix.M14 - secondMatrix.M14);
            distance += Math.Abs(firstMatrix.M21 - secondMatrix.M21);
            distance += Math.Abs(firstMatrix.M22 - secondMatrix.M22);
            distance += Math.Abs(firstMatrix.M23 - secondMatrix.M23);
            distance += Math.Abs(firstMatrix.M24 - secondMatrix.M24);
            distance += Math.Abs(firstMatrix.M31 - secondMatrix.M31);
            distance += Math.Abs(firstMatrix.M32 - secondMatrix.M32);
            distance += Math.Abs(firstMatrix.M33 - secondMatrix.M33);
            distance += Math.Abs(firstMatrix.M34 - secondMatrix.M34);
            distance += Math.Abs(firstMatrix.M41 - secondMatrix.M41);
            distance += Math.Abs(firstMatrix.M42 - secondMatrix.M42);
            distance += Math.Abs(firstMatrix.M43 - secondMatrix.M43);
            distance += Math.Abs(firstMatrix.M44 - secondMatrix.M44);

            return distance;
        }

        private static void FixMainEntryDeformation(FPF_File fpfFile, ESK_File sourceSkeleton, ESK_File targetSkeleton, string sourceSkeletonPath)
        {
            FPF_Entry mainEntry = fpfFile.GetMainSkeletonEntry();

            if (mainEntry == null)
                throw new InvalidDataException("FPF file does not contain the main skeleton entry.");

            IList<ESK_Bone> sourceBones = GetFpfSourceBones(mainEntry, sourceSkeleton, sourceSkeletonPath, "FPF entry 0", true);
            IList<ESK_Bone> targetBones = targetSkeleton.Skeleton.NonRecursiveBones;
            mainEntry.BonePoses = FixFpfEntryDeformation(mainEntry, sourceBones, targetBones);

            Dictionary<string, int> targetBoneIndexes = new Dictionary<string, int>();

            for (int boneIndex = 0; boneIndex < targetBones.Count; boneIndex++)
                targetBoneIndexes.Add(targetBones[boneIndex].Name, boneIndex);

            fpfFile.BoneIndexes.Remap(sourceBones.Select(bone => bone.Name).ToList(), targetBoneIndexes);
            fpfFile.ValidateMainSkeleton(targetBones.Select(bone => bone.Name).ToList());
        }

        private static List<FPF_BonePose> FixFpfEntryDeformation(FPF_Entry entry, IList<ESK_Bone> sourceBones, IList<ESK_Bone> targetBones)
        {
            if (entry?.BonePoses == null || entry.BonePoses.Count != sourceBones.Count)
                throw new InvalidDataException($"FPF entry {entry?.ID ?? -1} has {entry?.BonePoses?.Count ?? 0} bone transforms, but the source skeleton has {sourceBones.Count} bones.");

            Dictionary<string, int> sourceIndexesByName = new Dictionary<string, int>();
            Dictionary<string, ESK_Bone> sourceBonesByName = new Dictionary<string, ESK_Bone>();

            for (int boneIndex = 0; boneIndex < sourceBones.Count; boneIndex++)
            {
                sourceIndexesByName.Add(sourceBones[boneIndex].Name, boneIndex);
                sourceBonesByName.Add(sourceBones[boneIndex].Name, sourceBones[boneIndex]);
            }

            Matrix4x4[] targetBindAbsoluteMatrices = GetAbsoluteBindMatrices(targetBones);
            Matrix4x4[] targetPoseAbsoluteMatrices = new Matrix4x4[targetBones.Count];
            List<FPF_BonePose> fixedPoses = new List<FPF_BonePose>();

            for (int boneIndex = 0; boneIndex < targetBones.Count; boneIndex++)
            {
                ESK_Bone targetBone = targetBones[boneIndex];
                Matrix4x4 targetBindLocalMatrix = GetRelativeMatrix(targetBone);
                Matrix4x4 targetLocalPoseMatrix = targetBindLocalMatrix;

                if (TryGetSourceBoneForTarget(targetBone, sourceBones, sourceIndexesByName, sourceBonesByName, out int sourceBoneIndex, out ESK_Bone sourceBone))
                {
                    Matrix4x4 sourceBindLocalMatrix = GetRelativeMatrix(sourceBone);
                    Matrix4x4 sourceLocalPoseMatrix = GetSourceAttachmentLocalPose(entry, sourceBoneIndex, sourceBone);
                    Matrix4x4 sourcePoseDelta = sourceLocalPoseMatrix * Invert(sourceBindLocalMatrix);
                    targetLocalPoseMatrix = sourcePoseDelta * targetBindLocalMatrix;
                }

                Matrix4x4 parentAbsolutePoseMatrix = GetParentMatrix(targetPoseAbsoluteMatrices, targetBone.Index1, boneIndex);
                Matrix4x4 targetAbsolutePoseMatrix = targetLocalPoseMatrix * parentAbsolutePoseMatrix;
                Matrix4x4 targetInverseBindMatrix = Invert(targetBindAbsoluteMatrices[boneIndex]);

                targetPoseAbsoluteMatrices[boneIndex] = targetAbsolutePoseMatrix;
                fixedPoses.Add(CreateFixedBonePose(boneIndex, targetBindLocalMatrix, targetLocalPoseMatrix, targetAbsolutePoseMatrix, targetInverseBindMatrix));
            }

            return fixedPoses;
        }

        private static void AddRootBoneAlias(IList<ESK_Bone> sourceBones, IList<ESK_Bone> targetBones, Dictionary<string, int> targetBoneIndexes)
        {
            ESK_Bone sourceRootBone = sourceBones.FirstOrDefault(bone => bone.Index1 < 0);
            ESK_Bone targetRootBone = targetBones.FirstOrDefault(bone => bone.Index1 < 0);

            if (sourceRootBone == null || targetRootBone == null || targetBoneIndexes.ContainsKey(sourceRootBone.Name))
                return;

            targetBoneIndexes.Add(sourceRootBone.Name, targetRootBone.Index);
        }

        private static bool TryGetSourceBoneForTarget(ESK_Bone targetBone, IList<ESK_Bone> sourceBones, Dictionary<string, int> sourceIndexesByName, Dictionary<string, ESK_Bone> sourceBonesByName, out int sourceBoneIndex, out ESK_Bone sourceBone)
        {
            if (sourceIndexesByName.TryGetValue(targetBone.Name, out sourceBoneIndex) && sourceBonesByName.TryGetValue(targetBone.Name, out sourceBone))
                return true;

            if (targetBone.Index1 < 0)
            {
                for (int boneIndex = 0; boneIndex < sourceBones.Count; boneIndex++)
                {
                    if (sourceBones[boneIndex].Index1 >= 0)
                        continue;

                    sourceBoneIndex = boneIndex;
                    sourceBone = sourceBones[boneIndex];
                    return true;
                }
            }

            sourceBoneIndex = -1;
            sourceBone = null;
            return false;
        }

        private static Matrix4x4 GetSourceAttachmentLocalPose(FPF_Entry entry, int sourceBoneIndex, ESK_Bone sourceBone)
        {
            Matrix4x4 sourceAbsolutePoseMatrix = ToMatrix(entry.BonePoses[sourceBoneIndex].AttachmentPoseTransform);

            if (sourceBone.Index1 >= sourceBoneIndex)
                throw new InvalidDataException($"Bone \"{sourceBone.Name}\" has an invalid parent index for FPF deformation fixing.");

            Matrix4x4 parentAbsolutePoseMatrix = sourceBone.Index1 >= 0
                ? ToMatrix(entry.BonePoses[sourceBone.Index1].AttachmentPoseTransform)
                : Matrix4x4.Identity;

            if (!Matrix4x4.Invert(parentAbsolutePoseMatrix, out Matrix4x4 inverseParentAbsolutePoseMatrix))
                return ToMatrix(entry.BonePoses[sourceBoneIndex].LocalPoseTransform);

            return sourceAbsolutePoseMatrix * inverseParentAbsolutePoseMatrix;
        }

        private static Matrix4x4[] GetAbsoluteBindMatrices(IList<ESK_Bone> bones)
        {
            Matrix4x4[] absoluteMatrices = new Matrix4x4[bones.Count];

            for (int boneIndex = 0; boneIndex < bones.Count; boneIndex++)
            {
                Matrix4x4 relativeMatrix = GetRelativeMatrix(bones[boneIndex]);
                Matrix4x4 parentMatrix = GetParentMatrix(absoluteMatrices, bones[boneIndex].Index1, boneIndex);

                absoluteMatrices[boneIndex] = relativeMatrix * parentMatrix;
            }

            return absoluteMatrices;
        }

        private static Matrix4x4 GetParentMatrix(Matrix4x4[] matrices, int parentIndex, int boneIndex)
        {
            if (parentIndex < 0)
                return Matrix4x4.Identity;

            if (parentIndex >= boneIndex)
                throw new InvalidDataException($"Bone {boneIndex} has an invalid parent index for FPF deformation fixing.");

            return matrices[parentIndex];
        }

        private static Matrix4x4 GetRelativeMatrix(ESK_Bone bone)
        {
            return ToMatrix(TransformMatrix4x4.FromRelativeTransform(bone.RelativeTransform));
        }

        private static FPF_BonePose CreateFixedBonePose(int boneIndex, Matrix4x4 relativeMatrix, Matrix4x4 localPoseMatrix, Matrix4x4 absolutePoseMatrix, Matrix4x4 inverseBindMatrix)
        {
            return new FPF_BonePose
            {
                Index = boneIndex,
                RelativeTransform = ToFpfMatrix(relativeMatrix),
                LocalPoseTransform = ToFpfMatrix(localPoseMatrix),
                AbsolutePoseTransform = ToFpfMatrix(absolutePoseMatrix),
                AttachmentPoseTransform = ToFpfMatrix(absolutePoseMatrix),
                FormationSkinningTransform = ToFpfMatrix(Matrix4x4.Transpose(inverseBindMatrix * absolutePoseMatrix))
            };
        }

        private static Matrix4x4 Invert(Matrix4x4 matrix)
        {
            if (!Matrix4x4.Invert(matrix, out Matrix4x4 inverse))
                throw new InvalidDataException("FPF deformation fix found a matrix that cannot be inverted.");

            return inverse;
        }

        private static Matrix4x4 ToMatrix(TransformMatrix4x4 matrix)
        {
            return new Matrix4x4(
                matrix.M11, matrix.M12, matrix.M13, matrix.M14,
                matrix.M21, matrix.M22, matrix.M23, matrix.M24,
                matrix.M31, matrix.M32, matrix.M33, matrix.M34,
                matrix.M41, matrix.M42, matrix.M43, matrix.M44);
        }

        private static TransformMatrix4x4 ToFpfMatrix(Matrix4x4 matrix)
        {
            return new TransformMatrix4x4
            {
                M11 = matrix.M11,
                M12 = matrix.M12,
                M13 = matrix.M13,
                M14 = matrix.M14,
                M21 = matrix.M21,
                M22 = matrix.M22,
                M23 = matrix.M23,
                M24 = matrix.M24,
                M31 = matrix.M31,
                M32 = matrix.M32,
                M33 = matrix.M33,
                M34 = matrix.M34,
                M41 = matrix.M41,
                M42 = matrix.M42,
                M43 = matrix.M43,
                M44 = matrix.M44
            };
        }

        private static string NormalizeRelativeGamePath(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? null : path.Replace('\\', '/').TrimStart('/');
        }

        private bool SaveSelectedFpf(string suffix, out string savePath)
        {
            savePath = null;

            SaveFileDialog saveFile = new SaveFileDialog
            {
                Filter = "FPF files (*.fpf)|*.fpf",
                FileName = Path.GetFileNameWithoutExtension(selectedFpfPath) + suffix + ".fpf",
                InitialDirectory = GetInitialSaveDirectory()
            };

            if (saveFile.ShowDialog(Window.GetWindow(this)) != true)
                return false;

            File.WriteAllBytes(saveFile.FileName, selectedFpfFile.Write().ToArray());
            savePath = saveFile.FileName;
            return true;
        }

        private sealed class SecondarySkeletonMatch
        {
            public string RelativePath { get; }
            public string AttachBone { get; }
            public int PartType { get; }
            public ESK_File SourceSkeleton { get; }
            public ESK_File TargetSkeleton { get; }

            public SecondarySkeletonMatch(string relativePath, string attachBone, int partType, ESK_File sourceSkeleton, ESK_File targetSkeleton)
            {
                RelativePath = relativePath;
                AttachBone = attachBone;
                PartType = partType;
                SourceSkeleton = sourceSkeleton;
                TargetSkeleton = targetSkeleton;
            }
        }

        private sealed class SourceSecondaryEntry
        {
            public SecondarySkeletonMatch Skeleton { get; }
            public FPF_Entry Entry { get; }

            public SourceSecondaryEntry(SecondarySkeletonMatch skeleton, FPF_Entry entry)
            {
                Skeleton = skeleton;
                Entry = entry;
            }
        }

        private sealed class FpfFileItem
        {
            public string RelativePath { get; }
            public string DisplayName => Path.GetFileName(RelativePath);

            public FpfFileItem(string relativePath)
            {
                RelativePath = relativePath;
            }
        }

        public sealed class AnimationItem
        {
            public EAN_Animation Animation { get; }
            public ushort ID => Animation.ID_UShort;
            public int Duration => Animation.FrameCount;
            public string Name => Animation.Name;

            public AnimationItem(EAN_Animation animation)
            {
                Animation = animation;
            }
        }

        public sealed class PreviewPoseItem
        {
            public int ID { get; }
            public string Name { get; }
            public FpfPreviewKind Kind { get; }

            public PreviewPoseItem(int id, string name, FpfPreviewKind kind)
            {
                ID = id;
                Name = name;
                Kind = kind;
            }
        }

        public enum FpfPreviewKind
        {
            Intro,
            Formation
        }
    }
}
