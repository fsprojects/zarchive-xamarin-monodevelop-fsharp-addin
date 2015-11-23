namespace MonoDevelop.FSharp

open System
open Gtk
open MonoDevelop
open MonoDevelop.Core
open MonoDevelop.Core.Text
open MonoDevelop.AnalysisCore
open MonoDevelop.Ide.Editor.Extension
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.Gui
open MonoDevelop.Ide.Gui.Components
open MonoDevelop.Ide.Gui.Content
open MonoDevelop.Ide.TypeSystem
open MonoDevelop.Ide

open FSharpLint.Application
type EditorMarkers =
  { Editor: TextEditor;  Markers : IGenericTextSegmentMarker seq }

type FSLintTextEditorExtension() =
  inherit TextEditorExtension()
  let mutable handler : IDisposable = null
  //let mutable markers : (IGenericTextSegmentMarker seq) = Seq.empty
//  let mutable markers : EditorMarkers = { Editor = base.Editor; Markers= Seq.empty }

  member private x.HandleDocumentParsed _ =
    if not x.Editor.IsInAtomicUndo then
      let document = x.DocumentContext.ParsedDocument
      if AnalysisOptions.EnableFancyFeatures.Value && document <> null && not Debugger.DebuggingService.IsDebugging then
//        let createMarker(warning: LintWarning.Warning) =
//          let startPos = x.Editor.LocationToOffset(warning.Range.StartLine, warning.Range.StartColumn) + 1
//          let endPos = x.Editor.LocationToOffset(warning.Range.EndLine, warning.Range.EndColumn) + 1
//          let segment = TextSegment.FromBounds(startPos, endPos)
//          //let location = x.Editor.OffsetToLocation(offset)
//          LoggingService.LogDebug ("FSLint Warning - " + warning.Info)
//       //   let location = new DocumentLocation(start.Line, start.Column)
//          //let effect = if warning. ? TextSegmentMarkerEffect.DottedLine : TextSegmentMarkerEffect.WavedLine;
//          let marker = TextMarkerFactory.CreateGenericTextSegmentMarker (x.Editor, TextSegmentMarkerEffect.WavedLine, segment);
//          //let tag = TextMarkerFactory.CreateSmartTagMarker (x.Editor, 5, location)
//          marker.IsVisible <- true
//          marker.Tag <- warning
//          marker.Color <- (DefaultSourceEditorOptions.Instance.GetColorStyle()).UnderlineSuggestion.Color
//         
//          marker
//          //markers
      
        let addMarkers() =
//          for marker in markers do
//            x.Editor.RemoveMarker(marker) |> ignore
          match document.TryGetAst() with
          | Some ast ->
              match ast.ParseTree with
              | Some tree -> 
                  let result =
                      Lint.lintParsedSource
                        Lint.OptionalLintParameters.Default
                        { Ast = tree
                          Source = x.Editor.Text
                          TypeCheckResults = ast.CheckResults
                          FSharpVersion = Version() }
      //Error(errorType, String.wrapText error.Message 80, DocumentRegion (error.StartLineAlternate, error.StartColumn + 1, error.EndLineAlternate, error.EndColumn + 1))
                  //let warnings =
                  let formatError (warning: LintWarning.Warning) =
                    Error(ErrorType.Warning, String.wrapText warning.Info 80, DocumentRegion (warning.Range.StartLine, warning.Range.StartColumn + 1, warning.Range.EndLine, warning.Range.EndColumn + 1))
                  match result with
                  | LintResult.Success warnings -> //markers.Markers |> Seq.iter(fun marker -> markers.Editor.RemoveMarker(marker) |> ignore)
                                                   let doc = x.DocumentContext.ParsedDocument :?> FSharpParsedDocument
                                                   warnings |> (Seq.map formatError >> doc.AddRange)
                                                   //markers <- { Editor = x.DocumentContext.ParsedDocument :> FSharpParsedDocument; Markers = warnings |> Seq.map createMarker }
                                                   //markers.Markers |> Seq.iter x.Editor.AddMarker
                  | LintResult.Failure _ -> LoggingService.LogDebug "FSharpLint error"
                                            //Seq.empty
                                                
                  //()
              | None -> ()//Seq.empty
          | None -> ()//Seq.empty
        addMarkers()
        //Application.Invoke(fun _ _ -> addMarkers())

  override x.Initialize() =
    base.Initialize()
//    let tc = x.Editor.TextChanging.Subscribe(fun o e -> (markers.Markers 
//                                                         |> Seq.iter(fun marker -> markers.Editor.RemoveMarker(marker) |> ignore)))
//    let tc2 = x.Editor.DocumentContextChanged.Subscribe(fun o e -> (markers.Markers 
//                                                         |> Seq.iter(fun marker -> markers.Editor.RemoveMarker(marker) |> ignore)))


    handler <- x.DocumentContext.DocumentParsed.Subscribe(fun o e -> x.HandleDocumentParsed())

  override x.Dispose() =
    handler.Dispose()

