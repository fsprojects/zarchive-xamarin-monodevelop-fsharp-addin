namespace MonoDevelop.FSharp

open System
open System.Collections.Generic
open System.Linq
open System.Text.RegularExpressions
open Gtk
open Gdk
open GLib
open MonoDevelop.CodeActions
open MonoDevelop.Core
open MonoDevelop.Ide
open MonoDevelop.Ide.Gui.Dialogs
open MonoDevelop.Refactoring
open MonoDevelop.Components
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.TypeSystem
open FSharpLint.Application
open FSharpLint.Framework.Configuration

type analyserOrRule =
  | Analyzer of KeyValuePair<string, Analyser>
  | Rule of KeyValuePair<string, Rule>

type Row =
  { analyserOrRule : analyserOrRule
    enabled : bool }

type FSharpLintPanelWidget() as this =
  inherit Bin()
  //let row = { AnalyzerOrRule.Analyser analyser; false }
  let mutable vbox1 : VBox = null
  let mutable hbox1 : HBox = null
  let mutable GtkScrolledWindow : ScrolledWindow = null
  let mutable treeviewInspections : TreeView = null
  let mutable searchentryFilter : SearchEntry = null
  let fsLintSettings = "FSharpBinding.LintSettings"
  let config = PropertyService.Get(fsLintSettings, FSharpLint.Framework.Configuration.defaultConfiguration)

  let isRuleEnabled (settings : Map<string, Setting>) =
    if settings.ContainsKey "Enabled" then
      match settings.["Enabled"] with
      | Enabled(e) -> e
      | _ -> false
    else false

  let renderToggle _column (cellRenderer : CellRenderer) (treeModel : TreeModel) (iter : TreeIter) =
    let row = treeModel.GetValue(iter, 0) :?> Row
    let cellRenderer = (cellRenderer :?> CellRendererToggle)
    // let enabled = treeModel.GetValue(iter, 1) :?> bool
    match row.analyserOrRule with
    | Rule(_) ->
      cellRenderer.Visible <- true
      //cellRenderer.Active <- isRuleEnabled rule.Value.Settings
      cellRenderer.Active <- row.enabled
    | _ -> cellRenderer.Visible <- false

  let insertSpacesInCamelCase (s) = Regex.Replace(s, "([a-z](?=[A-Z])|[A-Z](?=[A-Z][a-z]))", "$1 ")

  let renderTitle _column (cellRenderer : CellRenderer) (treeModel : TreeModel) (iter : TreeIter) =
    let row = treeModel.GetValue(iter, 0) :?> Row
    let cellRenderer = (cellRenderer :?> CellRendererText)
    //    Song song = (Song) model.GetValue (iter, 0);
    //(cell as Gtk.CellRendererText).Text = song.Artist;
    //config.Analysers
    match row.analyserOrRule with
    | Analyzer(analyser) ->
      cellRenderer.Markup <- "<b>" + insertSpacesInCamelCase analyser.Key + "</b>"
    | Rule(rule) -> cellRenderer.Text <- insertSpacesInCamelCase rule.Key
    | _ -> cellRenderer.Visible <- true

  let treeStore = new TreeStore(typeof<Row>)

  do
    Stetic.Gui.Initialize(this) |> ignore
    // Widget MonoDevelop.CodeIssues.CodeIssuePanelWidget
    Stetic.BinContainer.Attach(this) |> ignore
    vbox1 <- new VBox()
    //vbox1.Name <- "vbox1"
    vbox1.Spacing <- 6
    // Container child vbox1.Gtk.Box+BoxChild
    hbox1 <- new HBox()
    //hbox1.Name = "hbox1"
    hbox1.Spacing <- 6
    // Container child hbox1.Gtk.Box+BoxChild
    searchentryFilter <- new SearchEntry()
    searchentryFilter.Name <- "searchentryFilter"
    //searchentryFilter.ForceFilterButtonVisible <- false
    searchentryFilter.HasFrame <- false
    searchentryFilter.RoundedShape <- false
    searchentryFilter.IsCheckMenu <- false
    searchentryFilter.ActiveFilterID <- 0
    searchentryFilter.Ready <- true
    searchentryFilter.HasFocus <- false
    searchentryFilter.Visible <- true
    hbox1.Add(searchentryFilter)
    //let w1 = ((BoxChild)(hbox1 [searchentryFilter]))
    let w1 = hbox1.[searchentryFilter] :?> Box.BoxChild
    w1.Position <- 0
    vbox1.Add hbox1
    let w2 = vbox1.[hbox1] :?> Box.BoxChild
    w2.Position <- 0
    w2.Expand <- false
    w2.Fill <- false
    // Container child vbox1.Gtk.Box+BoxChild
    GtkScrolledWindow <- new ScrolledWindow()
    //GtkScrolledWindow.Name = "GtkScrolledWindow"
    GtkScrolledWindow.ShadowType <- ShadowType.EtchedIn
    // Container child GtkScrolledWindow.Gtk.Container+ContainerChild
    treeviewInspections <- new TreeView()
    treeviewInspections.CanFocus <- true
    treeviewInspections.HeadersVisible <- false
    //treeviewInspections.Name = "treeviewInspections"
    GtkScrolledWindow.Add(treeviewInspections)
    vbox1.Add(GtkScrolledWindow)
    let w4 = vbox1.[GtkScrolledWindow] :?> Box.BoxChild
    w4.Position <- 1
    base.Add vbox1
    let toggleRenderer = new CellRendererToggle()
    let toggleColumn = new Gtk.TreeViewColumn()
    //toggleColumn.Title <- "Enabled"
    //toggleColumn.Sizing <- TreeViewColumnSizing.Autosize
    toggleColumn.PackStart(toggleRenderer, false)
    toggleRenderer.Activatable <- true
    //    toggleRenderer.Toggled.a
    toggleRenderer.Toggled.AddHandler(fun o args ->
      let mutable iter : TreeIter = new TreeIter()
      let res = treeStore.GetIterFromString(&iter, args.Path)
      //    TreeIter iter;
      if res then
        let row = treeStore.GetValue(iter, 0) :?> Row
        //let enabled = isRuleEnabled provider.Value.Settings
        //let enabled = treeStore.GetValue(iter, 1) :?> bool
        //provider.Value = new KeyValuePair<string, Rule>(provider.Key,
        // let rule = { provider.Value with Settings = Map.ofList [ ("Enabled", Enabled(not enabled)) ] }

        match row.analyserOrRule with
        | Rule(_) -> //row <- { row with enabled = not row.enabled }
                     treeStore.SetValue(iter, 0, { row with enabled = not row.enabled })
        | _ -> ()
    )
      
    //provider.Value <- { "Enabled", true }
    //{ provider.Value with Settings = Map.ofList [ ("Enabled", true) ] }
    treeviewInspections.AppendColumn(toggleColumn) |> ignore
    let titleColumn = new Gtk.TreeViewColumn()
    //titleColumn.Title <- "Title"
    let cellRenderer = new CellRendererText()
    titleColumn.PackStart(cellRenderer, true)
    titleColumn.AddAttribute(cellRenderer, "markup", 2)
    titleColumn.Expand <- true
    titleColumn.Sizing <- TreeViewColumnSizing.Autosize
    treeviewInspections.AppendColumn(titleColumn) |> ignore
    titleColumn.SetCellDataFunc(cellRenderer, new TreeCellDataFunc(renderTitle))
    //    let column2 = new Gtk.TreeViewColumn()
    //    //column2.Title <- "Title"
    //    treeviewInspections.AppendColumn(column2) |> ignore
    treeviewInspections.Model <- treeStore
    toggleColumn.SetCellDataFunc(toggleRenderer, new TreeCellDataFunc(renderToggle))
    for analyser in config.Analysers do
      let b = Analyzer analyser

      //      type AnalyserOrRule =
      //| Analyzer of KeyValuePair<string, Analyser>
      //| Rule of KeyValuePair<string, Rule>
      //
      //type Row = { analyserOrRule : AnalyserOrRule; enabled: bool; }
      let row =
        { analyserOrRule = Analyzer(analyser)
          enabled = false }

      let iter =
        treeStore.AppendValues([| { analyserOrRule = Analyzer(analyser)
                                    enabled = false } |])

      for rule in analyser.Value.Rules do
        let enabled = isRuleEnabled rule.Value.Settings
        treeStore.AppendValues(iter, ([| { analyserOrRule = Rule(rule)
                                           enabled = enabled } |])) |> ignore
    base.Child.ShowAll()
    vbox1.ShowAll()
    treeviewInspections.ShowAll()

  member x.ApplyChanges() =
    PropertyService.Set(fsLintSettings, config)
    IdeApp.Workbench.ReparseOpenDocuments()

type FSharpLintPanel() =
  inherit OptionsPanel()
  let mutable widget : FSharpLintPanelWidget option = None

  override x.CreatePanelWidget() =
    match widget with
    | Some widget -> widget :> Widget
    | None ->
      widget <- let w = new FSharpLintPanelWidget()
                Some w
      widget.Value :> Widget

  override x.ApplyChanges() = widget |> Option.iter (fun w -> w.ApplyChanges())
////
//// InspectionPanelWidget.cs
////
//// Author:
////       Mike Krüger <mkrueger@novell.com>
////
//// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
////
//// Permission is hereby granted, free of charge, to any person obtaining a copy
//// of this software and associated documentation files (the "Software"), to deal
//// in the Software without restriction, including without limitation the rights
//// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//// copies of the Software, and to permit persons to whom the Software is
//// furnished to do so, subject to the following conditions:
////
//// The above copyright notice and this permission notice shall be included in
//// all copies or substantial portions of the Software.
////
//// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//// THE SOFTWARE.
//using System;
//using MonoDevelop.Ide.Gui.Dialogs;
//using Gtk;
//using MonoDevelop.Core;
//using System.Linq;
//using MonoDevelop.Refactoring;
//using System.Collections.Generic;
//using GLib;
//using MonoDevelop.Components;
//using Gdk;
//using MonoDevelop.Ide.Editor;
//using MonoDevelop.CodeActions;
//using Microsoft.CodeAnalysis;
//using MonoDevelop.SourceEditor.QuickTasks;
//using MonoDevelop.Ide.TypeSystem;
//
//namespace MonoDevelop.CodeIssues
//{
//  class CodeIssuePanel : OptionsPanel
//  {
//    CodeIssuePanelWidget widget;
//
//    public CodeIssuePanelWidget Widget {
//      get {
//        EnsureWidget ();
//        return widget;
//      }
//    }
//
//    void EnsureWidget ()
//    {
//      if (widget != null)
//        return;
//      widget = new CodeIssuePanelWidget ("text/x-csharp");
//    }
//
//    public override Widget CreatePanelWidget ()
//    {
//      EnsureWidget ();
//      return widget;
//    }
//
//    public override void ApplyChanges ()
//    {
//      widget.ApplyChanges ();
//    }
//  }
//
//  partial class CodeIssuePanelWidget : Bin
//  {
//    readonly string mimeType;
//    readonly TreeStore treeStore = new TreeStore (typeof(string), typeof(Tuple<CodeDiagnosticDescriptor, DiagnosticDescriptor>), typeof (string));
//    readonly Dictionary<Tuple<CodeDiagnosticDescriptor, DiagnosticDescriptor>, DiagnosticSeverity?> severities = new Dictionary<Tuple<CodeDiagnosticDescriptor, DiagnosticDescriptor>, DiagnosticSeverity?> ();
//    readonly Dictionary<Tuple<CodeDiagnosticDescriptor, DiagnosticDescriptor>, bool> enableState = new Dictionary<Tuple<CodeDiagnosticDescriptor, DiagnosticDescriptor>, bool> ();
//
//    void GetAllSeverities ()
//    {
//      foreach (var node in BuiltInCodeDiagnosticProvider.GetBuiltInCodeDiagnosticDecsriptorsAsync (CodeRefactoringService.MimeTypeToLanguage (mimeType), true).Result) {
//        var root = new Tuple<CodeDiagnosticDescriptor, DiagnosticDescriptor> (node, null);
//        severities [root] = node.DiagnosticSeverity;
//        enableState [root] = node.IsEnabled;
//        if (node.GetProvider ().SupportedDiagnostics.Length > 1) {
//          foreach (var subIssue in node.GetProvider ().SupportedDiagnostics) {
//            var sub = new Tuple<CodeDiagnosticDescriptor, DiagnosticDescriptor> (node, subIssue);
//            severities [sub] = node.GetSeverity (subIssue);
//            enableState [sub] = node.GetIsEnabled (subIssue);
//          }
//        }
//      }
//    }
//
//    public void SelectCodeIssue (string idString)
//    {
//      TreeIter iter;
//      if (!treeStore.GetIterFirst (out iter))
//        return;
//      SelectCodeIssue (idString, iter);
//    }
//
//    bool SelectCodeIssue (string idString, TreeIter iter)
//    {
//      do {
//        var provider = (Tuple<CodeDiagnosticDescriptor, DiagnosticDescriptor>)treeStore.GetValue (iter, 1);
//        if (provider != null && idString  == provider.Item1.IdString) {
//          treeviewInspections.ExpandToPath (treeStore.GetPath (iter));
//          treeviewInspections.Selection.SelectIter (iter);
//          return true;
//        }
//
//        TreeIter childIterator;
//        if (treeStore.IterChildren (out childIterator, iter)) {
//          do {
//            if (SelectCodeIssue (idString, childIterator))
//              return true;
//          } while (treeStore.IterNext (ref childIterator));
//        }
//      } while (treeStore.IterNext (ref iter));
//      return false;
//    }
//
//    static string GetDescription (DiagnosticSeverity severity)
//    {
//      switch (severity) {
//      case DiagnosticSeverity.Hidden:
//        return GettextCatalog.GetString ("Do not show");
//      case DiagnosticSeverity.Error:
//        return GettextCatalog.GetString ("Error");
//      case DiagnosticSeverity.Warning:
//        return GettextCatalog.GetString ("Warning");
//      case DiagnosticSeverity.Info:
//        return GettextCatalog.GetString ("Info");
//      default:
//        throw new ArgumentOutOfRangeException ();
//      }
//    }
//
//    Xwt.Drawing.Image GetIcon (DiagnosticSeverity severity)
//    {
//      switch (severity) {
//      case DiagnosticSeverity.Error:
//        return QuickTaskOverviewMode.ErrorImage;
//      case DiagnosticSeverity.Warning:
//        return QuickTaskOverviewMode.WarningImage;
//      case DiagnosticSeverity.Info:
//        return QuickTaskOverviewMode.SuggestionImage;
//      default:
//        return QuickTaskOverviewMode.OkImage;
//      }
//    }
//
//    public void FillInspectors (string filter)
//    {
//      categories.Clear ();
//      treeStore.Clear ();
//      var grouped = severities.Keys
//        .Where (node => node.Item2 == null && (string.IsNullOrEmpty (filter) || node.Item1.Name.IndexOf (filter, StringComparison.OrdinalIgnoreCase) > 0))
//        .GroupBy (node => node.Item1.GetProvider ().SupportedDiagnostics.First ().Category)
//        .OrderBy (g => g.Key, StringComparer.Ordinal);
//
//      foreach (var g in grouped) {
//        TreeIter categoryIter = treeStore.AppendValues ("<b>" + g.Key + "</b>", null, null);
//        categories [g.Key] = categoryIter;
//
//        foreach (var node in g.OrderBy (n => n.Item1.Name, StringComparer.Ordinal)) {
//          var title = node.Item1.Name;
//          MarkupSearchResult (filter, ref title);
//          var nodeIter = treeStore.AppendValues (categoryIter, title, node, Ambience.EscapeText (node.Item1.Name));
//          if (node.Item1.GetProvider ().SupportedDiagnostics.Length > 1) {
//            foreach (var subIssue in node.Item1.GetProvider ().SupportedDiagnostics) {
//              title = subIssue.Title.ToString ();
//              MarkupSearchResult (filter, ref title);
//              treeStore.AppendValues (nodeIter, title, new Tuple<CodeDiagnosticDescriptor, DiagnosticDescriptor>(node.Item1, subIssue), Ambience.EscapeText (node.Item1.Name));
//            }
//          }
//        }
//      }
//      treeviewInspections.ExpandAll ();
//    }
//
//    public static void MarkupSearchResult (string filter, ref string title)
//    {
//      if (!string.IsNullOrEmpty (filter)) {
//        var idx = title.IndexOf (filter, StringComparison.OrdinalIgnoreCase);
//        if (idx >= 0) {
//          title =
//            Markup.EscapeText (title.Substring (0, idx)) +
//            "<span bgcolor=\"yellow\">" +
//            Markup.EscapeText (title.Substring (idx, filter.Length)) +
//            "</span>" +
//            Markup.EscapeText (title.Substring (idx + filter.Length));
//          return;
//        }
//      }
//      title = Markup.EscapeText (title);
//    }
//
//
//    class CustomCellRenderer : CellRendererCombo
//    {
//      public Xwt.Drawing.Image Icon {
//        get;
//        set;
//      }
//
//      protected override void Render (Gdk.Drawable window, Widget widget, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, Gdk.Rectangle expose_area, CellRendererState flags)
//      {
//        int w = 10;
//        var newCellArea = new Gdk.Rectangle (cell_area.X + w, cell_area.Y, cell_area.Width - w, cell_area.Height);
//        using (var ctx = CairoHelper.Create (window)) {
//          ctx.DrawImage (widget, Icon, cell_area.X - 4, cell_area.Y + Math.Round ((cell_area.Height - Icon.Height) / 2));
//        }
//
//        base.Render (window, widget, background_area, newCellArea, expose_area, flags);
//      }
//    }
//
//    public CodeIssuePanelWidget (string mimeType)
//    {
//      this.mimeType = mimeType;
//      // ReSharper disable once DoNotCallOverridableMethodsInConstructor
//      Build ();
//
//      // ensure selected row remains visible
//      treeviewInspections.SizeAllocated += (o, args) => {
//        TreeIter iter;
//        if (treeviewInspections.Selection.GetSelected (out iter)) {
//          var path = treeviewInspections.Model.GetPath (iter);
//          treeviewInspections.ScrollToCell (path, treeviewInspections.Columns[0], false, 0f, 0f);
//        }
//      };
//      treeviewInspections.TooltipColumn = 2;
//      treeviewInspections.HasTooltip = true;
//
//      var toggleRenderer = new CellRendererToggle ();
//      toggleRenderer.Toggled += delegate(object o, ToggledArgs args) {
//        TreeIter iter;
//        if (treeStore.GetIterFromString (out iter, args.Path)) {
//          var provider = (Tuple<CodeDiagnosticDescriptor, DiagnosticDescriptor>)treeStore.GetValue (iter, 1);
//          enableState[provider] = !enableState[provider];
//        }
//      };
//
//      var titleCol = new TreeViewColumn ();
//      treeviewInspections.AppendColumn (titleCol);
//      titleCol.PackStart (toggleRenderer, false);
//      titleCol.Sizing = TreeViewColumnSizing.Autosize;
//      titleCol.SetCellDataFunc (toggleRenderer, delegate (TreeViewColumn treeColumn, CellRenderer cell, TreeModel model, TreeIter iter) {
//        var provider = (Tuple<CodeDiagnosticDescriptor, DiagnosticDescriptor>)treeStore.GetValue (iter, 1);
//        if (provider == null) {
//          toggleRenderer.Visible = false;
//          return;
//        }
//        toggleRenderer.Visible = true;
//        toggleRenderer.Active = enableState[provider];
//      });
//
//
//      var cellRendererText = new CellRendererText {
//        Ellipsize = Pango.EllipsizeMode.End
//      };
//      titleCol.PackStart (cellRendererText, true);
//      titleCol.AddAttribute (cellRendererText, "markup", 0);
//      titleCol.Expand = true;
//
//      searchentryFilter.ForceFilterButtonVisible = true;
//      searchentryFilter.RoundedShape = true;
//      searchentryFilter.HasFrame = true;
//      searchentryFilter.Ready = true;
//      searchentryFilter.Visible = true;
//      searchentryFilter.Entry.Changed += ApplyFilter;
//
//
//      var comboRenderer = new CustomCellRenderer {
//        Alignment = Pango.Alignment.Center
//      };
//      var col = treeviewInspections.AppendColumn ("Severity", comboRenderer);
//      col.Sizing = TreeViewColumnSizing.GrowOnly;
//      col.MinWidth = 100;
//      col.Expand = false;
//
//      var comboBoxStore = new ListStore (typeof(string), typeof(DiagnosticSeverity));
////      comboBoxStore.AppendValues (GetDescription (Severity.None), Severity.None);
//      comboBoxStore.AppendValues (GetDescription (DiagnosticSeverity.Error), DiagnosticSeverity.Error);
//      comboBoxStore.AppendValues (GetDescription (DiagnosticSeverity.Warning), DiagnosticSeverity.Warning);
//      comboBoxStore.AppendValues (GetDescription (DiagnosticSeverity.Info), DiagnosticSeverity.Info);
//      comboRenderer.Model = comboBoxStore;
//      comboRenderer.Mode = CellRendererMode.Activatable;
//      comboRenderer.TextColumn = 0;
//
//      comboRenderer.Editable = true;
//      comboRenderer.HasEntry = false;
//
//      comboRenderer.Edited += delegate(object o, EditedArgs args) {
//        TreeIter iter;
//        if (!treeStore.GetIterFromString (out iter, args.Path))
//          return;
//
//        TreeIter storeIter;
//        if (!comboBoxStore.GetIterFirst (out storeIter))
//          return;
//        do {
//          if ((string)comboBoxStore.GetValue (storeIter, 0) == args.NewText) {
//            var provider = (Tuple<CodeDiagnosticDescriptor, DiagnosticDescriptor>)treeStore.GetValue (iter, 1);
//            var severity = (DiagnosticSeverity)comboBoxStore.GetValue (storeIter, 1);
//            severities[provider] = severity;
//            return;
//          }
//        } while (comboBoxStore.IterNext (ref storeIter));
//      };
//
//      col.SetCellDataFunc (comboRenderer, delegate (TreeViewColumn treeColumn, CellRenderer cell, TreeModel model, TreeIter iter) {
//        var provider = (Tuple<CodeDiagnosticDescriptor, DiagnosticDescriptor>)treeStore.GetValue (iter, 1);
//        if (provider == null) {
//          comboRenderer.Visible = false;
//          return;
//        }
//        var severity = severities[provider];
//        if (!severity.HasValue) {
//          comboRenderer.Visible = false;
//          return;
//        }
//        comboRenderer.Visible = true;
//        comboRenderer.Text = GetDescription (severity.Value);
//        comboRenderer.Icon = GetIcon (severity.Value);
//      });
//      treeviewInspections.HeadersVisible = false;
//      treeviewInspections.Model = treeStore;
//      GetAllSeverities ();
//      FillInspectors (null);
//    }
//
//    void ApplyFilter (object sender, EventArgs e)
//    {
//      FillInspectors (searchentryFilter.Entry.Text.Trim ());
//    }
//
//    readonly Dictionary<string, TreeIter> categories = new Dictionary<string, TreeIter> ();
//
//
//    public void ApplyChanges ()
//    {
//      foreach (var kv in severities) {
//        var userSeverity = kv.Value;
//        if (!userSeverity.HasValue)
//          continue;
//        if (kv.Key.Item2 == null) {
//          kv.Key.Item1.DiagnosticSeverity = userSeverity;
//          continue;
//        }
//        kv.Key.Item1.SetSeverity (kv.Key.Item2, userSeverity.Value);
//      }
//
//      foreach (var kv in enableState) {
//        var userIsEnabled = kv.Value;
//        if (kv.Key.Item2 == null) {
//          kv.Key.Item1.IsEnabled = userIsEnabled;
//          continue;
//        }
//        kv.Key.Item1.SetIsEnabled (kv.Key.Item2, userIsEnabled);
//      }
//    }
//  }
//}
