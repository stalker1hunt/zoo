using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ZooWorld.Editor
{
    public class AnimalDatabaseWindow : EditorWindow
    {
        [SerializeField] private AnimalDatabase database;
        [SerializeField] private AnimalDefinition selectedDefinition;
        [SerializeField] private string search = "";
        private VisualElement content;
        private VisualElement details;
        private Label status;
        private ListView speciesList;
        private ToolbarSearchField searchField;
        private ScrollView inspector;
        private AnimalDefinition displayedDefinition;
        private Button removeButton;
        private bool showingForm;
        private bool refreshingCatalog;
        private readonly List<AnimalDefinition> visibleSpecies = new List<AnimalDefinition>();

        [MenuItem("Zoo World/Animal Database")]
        public static void Open() => GetWindow<AnimalDatabaseWindow>("Animal Database");

        private void OnEnable()
        {
            minSize = new Vector2(400, 260);
            Undo.undoRedoPerformed += Rebuild;
            EditorApplication.projectChanged += RefreshCatalog;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= Rebuild;
            EditorApplication.projectChanged -= RefreshCatalog;
        }

        public void CreateGUI()
        {
            rootVisualElement.Unbind();
            rootVisualElement.Clear();
            rootVisualElement.AddToClassList("animal-database");
            StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/ZooWorld/Editor/AnimalDatabaseWindow.uss");
            if (sheet != null && !rootVisualElement.styleSheets.Contains(sheet)) rootVisualElement.styleSheets.Add(sheet);
            if (database == null)
            {
                string[] databases = AssetDatabase.FindAssets("t:AnimalDatabase");
                if (databases.Length == 1) database = AssetDatabase.LoadAssetAtPath<AnimalDatabase>(AssetDatabase.GUIDToAssetPath(databases[0]));
            }
            var header = new VisualElement();
            header.AddToClassList("database-header");
            var field = new ObjectField("Database") { name = "database", objectType = typeof(AnimalDatabase), value = database, allowSceneObjects = false };
            field.AddToClassList("database-selector");
            field.RegisterValueChangedCallback(change => SetDatabase((AnimalDatabase)change.newValue));
            header.Add(field);
            header.Add(new Button(Rebuild) { text = "Refresh" });
            header.Add(new Button(Validate) { text = "Validate" });
            rootVisualElement.Add(header);
            status = new Label { name = "status" };
            status.AddToClassList("database-status");
            rootVisualElement.Add(status);
            content = new VisualElement();
            content.AddToClassList("database-content");
            rootVisualElement.Add(content);
            Rebuild();
        }

        public void SetDatabase(AnimalDatabase value)
        {
            database = value;
            selectedDefinition = null;
            search = "";
            rootVisualElement.Q<ObjectField>("database")?.SetValueWithoutNotify(value);
            Rebuild();
        }

        private void Rebuild()
        {
            if (content == null) return;
            content.Unbind();
            content.Clear();
            speciesList = null;
            SetStatus("");
            if (database == null) return;
            var tabs = new Toolbar();
            tabs.Add(new ToolbarButton(ShowSpecies) { text = "Species" });
            tabs.Add(new ToolbarButton(ShowInteractions) { text = "Interactions" });
            content.Add(tabs);
            details = new VisualElement();
            details.AddToClassList("database-content");
            content.Add(details);
            ShowSpecies();
        }

        private void ShowSpecies()
        {
            showingForm = false;
            details.Unbind();
            details.Clear();
            displayedDefinition = null;
            var split = new TwoPaneSplitView(0, 180, TwoPaneSplitViewOrientation.Horizontal) { name = "speciesSplit" };
            var catalog = new VisualElement { name = "catalog" };
            catalog.AddToClassList("species-catalog");
            searchField = new ToolbarSearchField { name = "speciesSearch", value = search };
            searchField.AddToClassList("species-search");
            searchField.tooltip = "Search species by name";
            searchField.RegisterValueChangedCallback(change => { search = change.newValue; RefreshCatalog(); });
            catalog.Add(searchField);

            var actions = new VisualElement();
            actions.AddToClassList("species-actions");
            var add = new ToolbarMenu { name = "addSpecies", text = "Add" };
            add.menu.AppendAction("Existing species…", _ => ShowAddExisting());
            add.menu.AppendAction("Create new species…", _ => ShowCreateSpecies());
            actions.Add(add);
            removeButton = new Button(RemoveSelected) { name = "removeSpecies", text = "Remove", tooltip = "Remove from this database; keep the asset." };
            actions.Add(removeButton);
            catalog.Add(actions);

            speciesList = new ListView(visibleSpecies, 26, () => new Label(), (element, index) =>
            {
                AnimalDefinition definition = visibleSpecies[index];
                ((Label)element).text = definition == null ? "Missing species" : definition.DisplayName + " · " + definition.Role;
                element.AddToClassList("species-row");
            }) { name = "speciesList", selectionType = SelectionType.Single, reorderMode = ListViewReorderMode.Animated };
            speciesList.AddToClassList("species-list");
            speciesList.selectionChanged += _ =>
            {
                if (refreshingCatalog) return;
                showingForm = false;
                if (speciesList.selectedIndex >= 0) selectedDefinition = speciesList.selectedItem as AnimalDefinition;
                ShowSelected();
            };
            speciesList.itemIndexChanged += (from, to) => AnimalDatabaseAuthoring.MoveSpecies(database, from, to);
            catalog.Add(speciesList);
            inspector = new ScrollView(ScrollViewMode.Vertical) { name = "speciesInspector" };
            inspector.AddToClassList("species-inspector");
            split.Add(catalog);
            split.Add(inspector);
            details.Add(split);
            if (selectedDefinition == null) selectedDefinition = database.Definitions.FirstOrDefault(entry => entry != null);
            RefreshCatalog();
        }

        private void RefreshCatalog()
        {
            if (speciesList == null || database == null) return;
            refreshingCatalog = true;
            visibleSpecies.Clear();
            visibleSpecies.AddRange(database.Definitions.Where(entry => string.IsNullOrEmpty(search)
                || (entry != null && (entry.DisplayName ?? "").IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)));
            speciesList.reorderable = string.IsNullOrEmpty(search);
            speciesList.Rebuild();
            int index = selectedDefinition != null ? visibleSpecies.IndexOf(selectedDefinition) : -1;
            speciesList.SetSelectionWithoutNotify(index >= 0 ? new[] { index } : Array.Empty<int>());
            refreshingCatalog = false;
            if (!showingForm) ShowSelected();
        }

        private void ShowSelected()
        {
            bool selected = speciesList.selectedIndex >= 0;
            bool lastSpecies = selectedDefinition != null && database.Definitions.Count(entry => entry != null) <= 1;
            removeButton.SetEnabled(selected && !lastSpecies);
            removeButton.tooltip = lastSpecies ? "Keep at least one species in the database." : "Remove from this database; keep the asset.";
            AnimalDefinition definition = selected ? speciesList.selectedItem as AnimalDefinition : null;
            if (definition == displayedDefinition && inspector.childCount > 0) return;
            inspector.Unbind();
            inspector.Clear();
            displayedDefinition = definition;
            if (definition == null) return;
            inspector.Add(new InspectorElement(definition));
            inspector.Add(new Button(() => { Selection.activeObject = definition.Prefab; EditorGUIUtility.PingObject(definition.Prefab); }) { text = "Select animal prefab" });
            inspector.TrackSerializedObjectValue(new SerializedObject(definition), _ => RefreshCatalog());
        }

        private void RemoveSelected()
        {
            try
            {
                AnimalDefinition definition = speciesList.selectedItem as AnimalDefinition;
                int index = database.Definitions.ToList().IndexOf(definition);
                AnimalDatabaseAuthoring.RemoveSpecies(database, definition);
                showingForm = false;
                selectedDefinition = database.Definitions[Mathf.Clamp(index, 0, database.Definitions.Count - 1)];
                RefreshCatalog();
                SetStatus("Removed from database. Asset kept.");
            }
            catch (Exception error) { SetStatus(error.Message); }
        }

        private void ShowAddExisting()
        {
            BeginForm("Add existing species");
            var field = new ObjectField("Species asset") { name = "existingDefinition", objectType = typeof(AnimalDefinition), allowSceneObjects = false };
            field.AddToClassList("species-form-field");
            inspector.Add(field);
            var add = new Button(() =>
            {
                try
                {
                    var definition = (AnimalDefinition)field.value;
                    AnimalDatabaseAuthoring.AddSpecies(database, definition);
                    SelectAdded(definition);
                }
                catch (Exception error) { SetStatus(error.Message); }
            }) { name = "addExisting", text = "Add to database" };
            add.SetEnabled(false);
            field.RegisterValueChangedCallback(change => add.SetEnabled(change.newValue != null));
            AddFormActions(add);
        }

        private void ShowCreateSpecies()
        {
            BeginForm("Create new species");
            var name = new TextField("Name") { name = "newSpeciesName", value = "New animal" };
            var role = new EnumField("Role", AnimalRole.Prey) { name = "newSpeciesRole" };
            var prefab = new ObjectField("Animal prefab (required)") { name = "newSpeciesPrefab", objectType = typeof(AnimalActor), allowSceneObjects = false };
            foreach (VisualElement field in new VisualElement[] { name, role, prefab })
            {
                field.AddToClassList("species-form-field");
                inspector.Add(field);
            }
            inspector.Add(new HelpBox("The prefab supplies the animal's appearance and movement.", HelpBoxMessageType.Info));
            var create = new Button(() =>
            {
                string path = EditorUtility.SaveFilePanelInProject("Create animal species", name.value.Trim(), "asset", "Save the new species asset.");
                if (!string.IsNullOrEmpty(path)) CreateSpeciesAtPath(path, name.value, (AnimalRole)role.value, (AnimalActor)prefab.value);
            }) { name = "createSpecies", text = "Create and add" };
            void UpdateButton() => create.SetEnabled(!string.IsNullOrWhiteSpace(name.value) && prefab.value != null);
            name.RegisterValueChangedCallback(_ => UpdateButton());
            prefab.RegisterValueChangedCallback(_ => UpdateButton());
            UpdateButton();
            AddFormActions(create);
        }

        private void CreateSpeciesAtPath(string path, string name, AnimalRole role, AnimalActor prefab)
        {
            try { SelectAdded(AnimalDatabaseAuthoring.CreateSpeciesAsset(database, path, prefab, name, role)); }
            catch (Exception error) { SetStatus(error.Message); }
        }

        private void BeginForm(string title)
        {
            showingForm = true;
            inspector.Unbind();
            inspector.Clear();
            displayedDefinition = null;
            SetStatus("");
            var heading = new Label(title);
            heading.AddToClassList("species-form-title");
            inspector.Add(heading);
        }

        private void AddFormActions(Button primary)
        {
            var actions = new VisualElement();
            actions.AddToClassList("species-form-actions");
            actions.Add(primary);
            actions.Add(new Button(() => { showingForm = false; displayedDefinition = null; inspector.Clear(); ShowSelected(); }) { text = "Cancel" });
            inspector.Add(actions);
        }

        private void SelectAdded(AnimalDefinition definition)
        {
            selectedDefinition = definition;
            showingForm = false;
            search = "";
            searchField.SetValueWithoutNotify("");
            displayedDefinition = null;
            inspector.Clear();
            RefreshCatalog();
            SetStatus("");
        }

        private void ShowInteractions()
        {
            speciesList = null;
            details.Unbind();
            details.Clear();
            SetStatus("");
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.Add(new HelpBox("Prey + prey: bounce. Predator + prey: predator eats. Predator + predator: one survives. Pair exceptions override these defaults.", HelpBoxMessageType.Info));
            var serialized = new SerializedObject(database);
            scroll.Add(new PropertyField(serialized.FindProperty("exceptions"), "Pair exceptions"));
            scroll.Bind(serialized);
            details.Add(scroll);
        }

        private void Validate()
        {
            if (database == null) return;
            try
            {
                database.ValidateConfiguration();
                foreach (AnimalDefinition definition in database.Definitions) AnimalPrefabValidation.Validate(definition.Prefab);
                SetStatus("Valid: " + database.Definitions.Count + " species.");
            }
            catch (Exception error) { SetStatus(error.Message); }
        }

        private void SetStatus(string message)
        {
            if (status == null) return;
            status.text = message;
            status.EnableInClassList("database-status-empty", string.IsNullOrEmpty(message));
        }
    }
}
