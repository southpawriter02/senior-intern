This is a comprehensive development roadmap for **"Mimir’s Well,"** designed to take you from an empty Visual Studio solution to a production-ready world-building suite.

The roadmap is structured into **Five Epochs** (Milestones), creating a logical flow from data structure to visualization, and finally to game engine integration.

---

### **Epoch I: The Roots (Infrastructure & Core Data)**
**Version 0.1.0 – 0.3.5**
*Focus: Building the engine that drives the tool, handling files, images, and basic object definitions.*

#### **v0.1.x: The Foundation (Project & UI Shell)**
*Setting up the "Container" for your work.*

*   **v0.1.0: The Solution Skeleton**
    *   **Goal:** A running MAUI Blazor Hybrid app.
    *   **Tech:** Solution setup (App, Core, Data projects), Dependency Injection wiring.
    *   **Feature:** **LiteDB Integration.** Verify the app can create, read, and write to a local `.db` file in `AppData`.
*   **v0.1.1: Project Management (The "Multiverse")**
    *   **Goal:** Support multiple settings or "Save Files."
    *   **Tech:** File Dialogs.
    *   **Feature:** **"New/Load World" Screen.** Instead of hardcoding one database, allow the user to create `MyGame.mimir` or `TestWorld.mimir`.
*   **v0.1.2: Configuration & Settings**
    *   **Goal:** Tell the tool where your Game Engine project lives.
    *   **Tech:** `appsettings.json` or a local UserPreferences table.
    *   **Feature:** **Asset Path Definition.** Input fields to define where Unity/Unreal is installed so the app knows where to export JSON later.
*   **v0.1.3: The "Mimir" UI Shell**
    *   **Goal:** The navigation framework.
    *   **Tech:** MudBlazor `MudLayout`, `MudDrawer` (Sidebar).
    *   **Feature:** **Navigation Structure.** Set up the sidebar links (Foundry, Bestiary, Codex, Map).
    *   **Feature:** **Theme Engine.** Implement the dark mode/Nordic color palette (Slate Grays, Runestone Blues, Ember Oranges).

#### **v0.2.x: The Hoard (Itemization & Media)**
*Creating the first data types and handling images.*

*   **v0.2.0: The Item Prototype (CRUD)**
    *   **Goal:** Create/Read/Update/Delete a basic item.
    *   **Tech:** `Item` Model (Guid Id, String Name, String Description).
    *   **Feature:** A generic "Edit Item" form with basic text validation.
*   **v0.2.1: The Media Manager**
    *   **Goal:** Handle images without bloating the database.
    *   **Tech:** `FileService` for copying images to a local `/Images` folder.
    *   **Feature:** **Image Picker.** Click to upload an icon/concept art. The app copies the file to the project folder and stores the *relative path* in the database (not the binary data).
*   **v0.2.2: The "Duality" Component**
    *   **Goal:** Reusable UI for the Cargo Cult "Truth vs. Myth" mechanic.
    *   **Tech:** A custom Razor Component (`<DualityField @bind-Reality="..." @bind-Myth="..." />`).
    *   **Feature:** Creates a standardized UI block with two text areas and a visual divider, used later for Items, Locations, and NPCs.
*   **v0.2.3: Taxonomy & Tagging**
    *   **Goal:** Organize data efficiently.
    *   **Tech:** `Tag` Model, `Category` Enum.
    *   **Feature:** **Universal Tag System.** Create a "chip" based tag editor (e.g., "Radioactive", "Heavy", "QuestItem") that can be applied to any object type.
*   **v0.2.4: Material Primitives**
    *   **Goal:** Define what items are made of (for the economy).
    *   **Tech:** `Material` Model.
    *   **Feature:** Define base materials (Plastic, Copper, Circuitry).
    *   **Feature:** **Composition Editor.** A UI to say "This Laser Rifle = 3x Plastic, 1x Circuitry."

#### **v0.3.x: The Bestiary & Integrity (Validation & Stability)**
*Ensuring the data makes sense and connecting objects together.*

*   **v0.3.0: The Entity Core**
    *   **Goal:** Define living things.
    *   **Tech:** `Creature` Model inheriting from a base `Entity` class.
    *   **Feature:** Stat Block Editor (Health, Stamina, Radiation Resistance).
*   **v0.3.1: The Validation Engine**
    *   **Goal:** Prevent bad data entry.
    *   **Tech:** `FluentValidation` library.
    *   **Feature:** **Ruleset Implementation.** Enforce logic: "Max Health cannot be negative," "Name is required," "ID must be unique." Display error messages in the UI forms.
*   **v0.3.2: Reference Integrity (The "Soft Link" System)**
    *   **Goal:** Handle relationships between objects.
    *   **Tech:** GUID referencing.
    *   **Feature:** **Entity Picker.** A dropdown/modal that lets you select an `Item` to add to a `Creature's` inventory.
    *   **Feature:** **Orphan Protection.** If you try to delete an `Item`, check if any `Creature` is holding it. Warn the user: *"This item is used by 5 creatures. Delete anyway?"*
*   **v0.3.3: Loot Logic (Buckets)**
    *   **Goal:** Abstract loot generation.
    *   **Tech:** `LootTable` Model.
    *   **Feature:** **Drop Buckets.** Create groups of items (e.g., "Tier 1 Scrap"). Assign these buckets to creatures instead of individual items for easier balancing.
*   **v0.3.4: Auto-Save & Recovery**
    *   **Goal:** Prevent data loss during crashes.
    *   **Tech:** Background Service / Timer.
    *   **Feature:** **The Scribe.** Every 5 minutes (or on every "Save" action), copy the `.db` file to a `/Backups` folder with a timestamp.
*   **v0.3.5: The Search Dashboard**
    *   **Goal:** Find things quickly as the DB grows.
    *   **Tech:** LINQ Queries.
    *   **Feature:** **Global Quick-Nav.** A search bar in the top header that queries Items, Creatures, and Lore simultaneously and provides autocomplete results.

---

### **Epoch II: The Trunk (World, Society & Scripture)**
**Version 0.4.0 – 0.6.5**
*Focus: Populating the world with culture, geography, and the "Cargo Cult" narrative logic.*

#### **v0.4.x: The Tribes (Sociology & Faction Logic)**
*Defining who lives in the world and how they organize.*

*   **v0.4.0: The Faction Core**
    *   **Goal:** Create the container for groups of people.
    *   **Tech:** `Faction` Model, Basic CRUD Forms.
    *   **Feature:** Define "Archetypes" (e.g., Raiders, Settlers, Cultists) and "Aesthetics" (e.g., Rust & Bone, Neon & Plastic).
*   **v0.4.1: The Hierarchy Tree**
    *   **Goal:** Define the command structure within a faction.
    *   **Tech:** Recursive Data Structures, **MudTreeView** component.
    *   **Feature:** Create a visual tree for ranks (e.g., *High Jarl* → *Runeseer* → *Scavenger*). Assign generic "Minion Types" to these ranks for combat balancing.
*   **v0.4.2: The Cultural Dictionary**
    *   **Goal:** Standardize how factions speak and what they name things.
    *   **Tech:** String manipulation services, Random Seed Generators.
    *   **Feature:** **Dialect Manager.** Create a glossary of slang specific to a faction (e.g., *Faction A calls electricity "Thor’s Blood"; Faction B calls it "The Spark"*).
    *   **Feature:** **NPC Name Generator.** Configure specific naming rules per faction (e.g., [Norse Prefix] + [Industrial Suffix] = "Sigurd-9").
*   **v0.4.3: The Diplomacy Matrix**
    *   **Goal:** Define inter-faction politics.
    *   **Tech:** Matrix/Grid Logic, Enum flags (`AtWar`, `Trade`, `Schism`).
    *   **Feature:** A dynamic grid view. Clicking the intersection of "Cult of Odin" and "The NASA Scientists" allows you to set their relationship status and add notes on *why* they hate each other.

#### **v0.5.x: The Realms (Geography & Atmosphere)**
*Defining the physical stage where the game happens.*

*   **v0.5.0: The Biome Definitions**
    *   **Goal:** Define the environmental rules of different zones.
    *   **Tech:** `Biome` Model, Color Pickers for UI.
    *   **Feature:** Create distinct biomes (e.g., *The Rust Sea*, *The Glitched Forest*).
    *   **Feature:** **Atmosphere Settings.** Define fog density, skybox hex colors, and ambient sound IDs for each biome (for later export to Unity/Unreal).
*   **v0.5.1: Hazards & Afflictions**
    *   **Goal:** Gamify the geography.
    *   **Tech:** `Hazard` Model linked to `Biome`.
    *   **Feature:** Define environmental threats (Radiation, Extreme Cold, Nanite Swarms) and their intensity levels (1-10) per region.
*   **v0.5.2: The Ruin Architect (Dungeons)**
    *   **Goal:** Catalogue the "Old World" locations found in these biomes.
    *   **Tech:** `Location` Model with `ParentBiomeId`.
    *   **Feature:** **Dungeon Archetypes.** Define location types (Bunker, Mall, Server Farm).
    *   **Feature:** **Scavenge Tables.** Assign specific "Junk Categories" to specific Ruins (e.g., *Server Farms drop Circuitry; Malls drop Cloth*).
*   **v0.5.3: Resource Node Logic**
    *   **Goal:** Create conflict points for factions.
    *   **Tech:** Resource Entities.
    *   **Feature:** Place static resource nodes (Water, Fuel, Ammo Factories) into Locations. This will help you write plot hooks later (e.g., *Faction A attacks Location B because it has a Water node*).

#### **v0.6.x: The Codex (Lore, Truth & Rituals)**
*Implementing the "Cargo Cult" mechanics and narrative depth.*

*   **v0.6.0: The Scripture Editor**
    *   **Goal:** A place to write the lore.
    *   **Tech:** Markdown Editor implementation (e.g., `MudMarkdown`).
    *   **Feature:** Standard Rich Text entry for writing histories and myths.
*   **v0.6.1: The Duality Engine (Truth vs. Myth)**
    *   **Goal:** The core "Cargo Cult" feature.
    *   **Tech:** Split-view UI.
    *   **Feature:** Apply the "Two-Description" system to *Locations* and *Events*, not just Items.
    *   **Feature:** **"The Misinterpretation Generator."** A button that takes a keyword (e.g., "Nuclear Reactor") and suggests a mythological twist (e.g., "The Sleeping Dragon's Heart").
*   **v0.6.2: The Ritual Builder**
    *   **Goal:** Gameplay-ify the lore.
    *   **Tech:** Sequential List Editor (Drag and Drop reordering).
    *   **Feature:** Define the "Steps of Operation" for tech items.
        1.  *Input:* "Insert Keycard" -> *Cult Belief:* "Offer the tablet of stone."
        2.  *Input:* "Type Password" -> *Cult Belief:* "Recite the prayer of access."
*   **v0.6.3: The Prophecy Log (Quest Prerequisites)**
    *   **Goal:** Structure the narrative timeline.
    *   **Tech:** Timeline Component.
    *   **Feature:** Define "World States." (e.g., *State: Winter Has Come*).
    *   **Feature:** Link Factions and Items to these states (e.g., *The "Flamethrower" item only unlocks in shops after the "Winter Has Come" event is triggered*).

---

### **Epoch III: The Branches (Visualization & Context)**
**Version 0.7.0 – 0.8.9**
*Focus: Node graphs, interactive maps, and visual storytelling tools.*

#### **v0.7.x: The Weave (Node Graph Systems)**
*Visualizing the abstract relationships between data points.*

*   **v0.7.0: The Diagram Engine**
    *   **Goal:** Initialize the canvas.
    *   **Tech:** `Blazor.Diagrams` integration.
    *   **Feature:** **The Infinite Canvas.** Create a zoomable, pannable workspace.
    *   **Feature:** **Custom Node Components.** Create standard Blazor components that render inside the graph nodes (e.g., a "Character Node" showing a portrait, name, and health bar).
*   **v0.7.1: The Genealogy Builder (Hierarchy)**
    *   **Goal:** Visualize Faction structures.
    *   **Tech:** Tree Layout Algorithm.
    *   **Feature:** **Auto-Layout.** A button that takes a selected Faction and automatically arranges its members in a pyramid structure (Leader -> Officers -> Grunts).
    *   **Feature:** **Drag-and-Drop Reassignment.** Drag a "Grunt" node to a different "Officer" node to update their `SupervisorId` in the database immediately.
*   **v0.7.2: The Diplomacy Web**
    *   **Goal:** Visualize the "Political Matrix" created in v0.4.3.
    *   **Tech:** Force-Directed Graph Layout.
    *   **Feature:** **Link Styling.** Draw lines between Factions. Color code them: Green (Trade), Red (War), Dotted/Grey (Unknown).
    *   **Feature:** **Thickness Logic.** Line thickness represents the *intensity* of the relationship (e.g., Total War = Thick Red Line).
*   **v0.7.3: The Crafting Tree (Tech Dependencies)**
    *   **Goal:** Balance the economy.
    *   **Tech:** Directed Acyclic Graph (DAG).
    *   **Feature:** **Reverse Lookup.** Select a "Laser Rifle" and generate a tree showing every raw material needed to make it, down to the base plastic and copper.
    *   **Feature:** **Missing Link Alert.** Highlight nodes in Red if a required component doesn't exist in the database yet.
*   **v0.7.4: The Quest Flowchart**
    *   **Goal:** Non-linear narrative design.
    *   **Tech:** Port logic (Input/Output dots on nodes).
    *   **Feature:** **Branching Logic.** Create nodes for "Dialogue," "Combat," and "Check." Connect "Success" and "Failure" outputs to different narrative nodes.
    *   **Feature:** **Cyclic Detection.** Validate that the quest doesn't loop infinitely (unless intended).

#### **v0.8.x: The Atlas (Geographic Mapping)**
*Visualizing the physical world and the "Duality" of the setting.*

*   **v0.8.0: The Map Core**
    *   **Goal:** Render the world map.
    *   **Tech:** HTML5 Canvas or SVG container.
    *   **Feature:** **Image Loading.** Load a high-res image of your game map (exported from Unity/Unreal or drawn in Photoshop).
    *   **Feature:** **Coordinate Translation.** Map the pixel coordinates (X: 1024, Y: 500) to Game World Coordinates (Vector3).
*   **v0.8.1: The Pinning System**
    *   **Goal:** Link the DB to the Map.
    *   **Tech:** Drag-and-Drop Interop.
    *   **Feature:** **Asset Drawer.** Open a sidebar of "Locations" from your DB. Drag "The Rusty Tower" onto the map to save its X/Y coordinates.
    *   **Feature:** **Quick-Edit.** Clicking a pin opens a modal to edit that location's data without leaving the map view.
*   **v0.8.2: The Layer Manager**
    *   **Goal:** Filter information.
    *   **Tech:** Z-Index / Visibility Toggles.
    *   **Feature:** **Toggle Groups.** Show/Hide pins based on category: "Show Resources," "Show Hazards," "Show Faction Strongholds."
*   **v0.8.3: Zone Painting (The Biomes)**
    *   **Goal:** visual definition of regions.
    *   **Tech:** SVG Polygons.
    *   **Feature:** **Polygon Tool.** Click to draw shapes over the map to define Biomes (e.g., outlining the "Radioactive Swamps").
    *   **Feature:** **Biome Inheritance.** Any Pin dropped inside this polygon automatically inherits the Biome ID and Hazard settings of that region.
*   **v0.8.4: The "Duality" Overlay (Cargo Cult View)**
    *   **Goal:** See the world as they see it.
    *   **Tech:** Image Blending / CSS Filters.
    *   **Feature:** **The Myth Lens.** A toggle switch.
        *   *Mode A (Reality):* Shows the satellite map, labels say "Subway Station," "Power Plant."
        *   *Mode B (Myth):* Applies a parchment/rune filter. Labels change to "The Underworld Gate," "The Temple of Lightning." (Uses the Duality text fields from v0.6.1).
*   **v0.8.5: Trade Route logic**
    *   **Goal:** Visualize movement.
    *   **Tech:** Bezier Curves.
    *   **Feature:** Draw lines between Settlements. Calculate the distance and estimate "Travel Time" based on terrain difficulty defined in the Biome.

#### **v0.8.x: The Oracle (Context & Analysis)**
*Tools that analyze the data to find balance issues.*

*   **v0.8.6: The Timeline (Chronology)**
    *   **Goal:** Visualize the sequence of events.
    *   **Tech:** Gantt Chart Component (e.g., `MudTimeline`).
    *   **Feature:** **Era Grouping.** Group events by "Pre-Collapse," "The Fall," and "Current Era."
    *   **Feature:** **Life-span Bars.** Visualize Character lives on the timeline to see who was alive during which event.
*   **v0.8.7: The Loot Distribution Graph**
    *   **Goal:** Economy balancing.
    *   **Tech:** `MudChart` (Pie/Bar).
    *   **Feature:** **Rarity Check.** A pie chart showing the ratio of Common/Rare/Legendary items. (If 50% of your items are Legendary, your game is broken).
*   **v0.8.8: The Bestiary Scale**
    *   **Goal:** Visual context for creature design.
    *   **Tech:** Silhouette Rendering.
    *   **Feature:** **Size Compare.** A view that renders the silhouette of a standard Human (1.8m) next to the selected Monster based on its height data, to help you visualize scale.
*   **v0.8.9: Snapshot Export**
    *   **Goal:** Documentation.
    *   **Tech:** `html2canvas` (JS Interop).
    *   **Feature:** **"Print to Codex."** A button that saves the current Map View or Node Graph as a high-res PNG to be included in your Game Design Document (GDD).

---

### **Epoch IV: The Bifrost (Game Engine Integration)**
**Version 0.9.0 – 0.9.9**
*Focus: Serialization, Code Generation, Asset Mapping, and Live-Sync pipelines.*

#### **v0.9.x: The Bridge (Data Pipeline)**
*Getting the data out of LiteDB and into the Game Project.*

*   **v0.9.0: The Schema Contract (POCO Library)**
    *   **Goal:** Share code between Tool and Game to prevent "Desync."
    *   **Tech:** Shared Class Library (`MyGame.Shared.dll`).
    *   **Feature:** **The Shared Kernel.** Move all Data Models (Item, Enemy, Faction) into a separate DLL project.
    *   **Feature:** **Build Script.** Configure Mimir to automatically copy this DLL into your Unity/Unreal `Plugins` folder on every build, ensuring the game always uses the exact same data structure as the tool.
*   **v0.9.1: The JSON Serializer (Raw Data Export)**
    *   **Goal:** Dump the database to readable text files.
    *   **Tech:** `System.Text.Json` with custom Converters.
    *   **Feature:** **Flat-File Database.** Export the entire LiteDB content into organized folders (e.g., `/StreamingAssets/Data/Items/*.json`).
    *   **Feature:** **Pretty Printing.** Ensure JSON is formatted with indentation so it is human-readable and Git-friendly (easy to see diffs in version control).
*   **v0.9.2: The Code Generator (T4 / Source Gen)**
    *   **Goal:** Stop writing Enums and ID lookups manually in the game.
    *   **Tech:** StringBuilders / .tt Templates.
    *   **Feature:** **Static ID Generation.** Generate a C# file (`GameIDs.cs`) containing static references to key items.
        *   *Result:* Instead of `GetItem("some-guid-string")`, you can type `Inventory.Add(GameIDs.Items.Mjolnir_Rifle)` in your game code.
    *   **Feature:** **Enum Syncer.** If you add a new "DamageType" in Mimir, this feature regenerates the `DamageType` enum file in the game project.
*   **v0.9.3: The Asset Linker (File Scraper)**
    *   **Goal:** Connect database entries to 3D models/Sounds without manual entry.
    *   **Tech:** `System.IO.FileSystemWatcher`.
    *   **Feature:** **The Crawler.** Point Mimir at your Game Project’s `Assets` folder. It scans for files.
    *   **Feature:** **Auto-Link.** If Mimir finds `Sword_Viking.fbx`, it suggests linking it to the "Viking Sword" item in the database. It stores the *relative path* (`Assets/Meshes/Weapons/Sword_Viking.fbx`).

#### **v0.9.x: The Translator (Duality & Localization)**
*Handling the specific "Cargo Cult" mechanics in the export.*

*   **v0.9.4: The String Table Builder (Localization)**
    *   **Goal:** Handle the "Truth vs. Myth" descriptions efficiently.
    *   **Tech:** CSV / Key-Value Pair Export.
    *   **Feature:** **Dual-Language Export.** Instead of putting text in the Item JSON, generate two localization files:
        *   `en_US_Reality.csv` (Contains technical descriptions).
        *   `en_US_Myth.csv` (Contains religious descriptions).
    *   **Feature:** **Key Generation.** Replace text in the JSON with keys (e.g., `DESC_ITEM_RIFLE_01`). The game engine swaps the CSV file based on the character's "Sanity/Belief" stat.
*   **v0.9.5: The Texture Baker (Rune Generation)**
    *   **Goal:** Procedural asset creation.
    *   **Tech:** `SkiaSharp` or `ImageSharp`.
    *   **Feature:** **Decal Export.** If you used the "Circuit-to-Rune" tool (v0.6.x), this step bakes that vector data into a transparent `.png` texture and saves it directly to the Game Texture folder to be used as a decal on in-game props.

#### **v0.9.x: The Logic Flow (Quest & Event Export)**
*Exporting the node graphs as game logic.*

*   **v0.9.6: The Logic Transpiler**
    *   **Goal:** Make the Node Graphs playable.
    *   **Tech:** Custom Interpreter or Export to Lua/C#.
    *   **Feature:** **Graph-to-Data.** Convert the "Quest Flowchart" (v0.7.4) into a lightweight node format the game can traverse at runtime.
    *   **Feature:** **Trigger Map.** Export a list of "Event Listeners." (e.g., "If Player enters Zone B, Check Condition X").
*   **v0.9.7: The Loot Table Compiler**
    *   **Goal:** Optimize RNG logic.
    *   **Tech:** Weighted Tree Flattening.
    *   **Feature:** **Pre-calculation.** Instead of making the game calculate drop rates every kill, Mimir pre-calculates "Drop Buckets" and exports a simplified array for performance.

#### **v0.9.x: The Watchman (Workflow Tools)**
*Improving the developer experience.*

*   **v0.9.8: The Hot-Reloader (Live Sync)**
    *   **Goal:** Tweak numbers while the game is running.
    *   **Tech:** Socket / File Watcher.
    *   **Feature:** **The Bifrost Link.** If the Game is running in the Editor, and you change an Item's Damage in Mimir and hit Save, Mimir writes the JSON.
    *   **Feature:** **Auto-Refresh.** The Game detects the file change and reloads the stats immediately without restarting.
*   **v0.9.9: The Validator (Build Safety)**
    *   **Goal:** Prevent broken builds.
    *   **Tech:** Unit Testing Logic.
    *   **Feature:** **Pre-Export Health Check.** Clicking "Export" runs a diagnostic:
        *   *Are there duplicate IDs?*
        *   *Do all Items point to valid icons?*
        *   *Are any required fields empty?*
    *   **Feature:** **The Gatekeeper.** Block the export if critical errors are found, preventing you from breaking the game with bad data.

---

### **Epoch V: The Crown (Polish & Release)**
**Version 1.0.0**
*Focus: UX, Stability, and "Quality of Life".*

#### **v0.9.5: The Auditor (Consistency Checkers)**
*   **Orphan Detection:**
    *   Build a dashboard that lists "Unused Assets" (Items with no drop source, Characters with no Faction).
*   **Timeline Logic:**
    *   Flag errors where a Character's death date is earlier than their birth date.
*   **Search Engine:**
    *   Implement Global Search (Regex supported) to find any text string across the entire database.

#### **v1.0.0: Mimir’s Well (Gold Release)**
*   **Batch Operations:**
    *   Select Multiple -> Mass Edit (e.g., Move 50 items to a new Faction).
*   **Backup System:**
    *   Auto-backup logic (zip the `.db` file every hour).
*   **Theming:**
    *   Finalize the UI theme (Dark/Nordic aesthetic).
*   **Launch:**
    *   Compile as a standalone Windows `.exe` (and Mac `.app` if needed).

---

### **Recommended "sprints"**
If you are working solo, I recommend treating each **0.x.0** version as a 1-to-2 week sprint.

1.  **Sprint 1:** Get the App running and able to save a simple "Sword" to a database.
2.  **Sprint 2:** Build the "Item" and "Faction" forms.
3.  **Sprint 3:** Build the JSON Export (Do this early! You want to see your data in Unity/Unreal ASAP to stay motivated).
4.  **Sprint 4:** Build the Visual Node Graph.
