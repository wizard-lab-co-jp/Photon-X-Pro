namespace PhotonXPro.Core.Orchestration

open System
open System.Text.RegularExpressions
open System.Threading.Tasks

module PdfWorkflow =
    
    // Natural Sort Logic (Pure function, thread-safe)
    let naturalSort (list: string list) =
        let regex = Regex(@"(\d+)|(\D+)", RegexOptions.Compiled)
        let compareNatural (s1: string) (s2: string) =
            let rec compare (m1: Match list) (m2: Match list) =
                match m1, m2 with
                | [], [] -> 0
                | [], _ -> -1
                | _, [] -> 1
                | h1::t1, h2::t2 ->
                    let v1, v2 = h1.Value, h2.Value
                    match Int32.TryParse v1, Int32.TryParse v2 with
                    | (true, n1), (true, n2) when n1 <> n2 -> n1.CompareTo(n2)
                    | _ when v1 <> v2 -> v1.CompareTo(v2)
                    | _ -> compare t1 t2
            
            let matches1 = regex.Matches(s1) |> Seq.cast<Match> |> Seq.toList
            let matches2 = regex.Matches(s2) |> Seq.cast<Match> |> Seq.toList
            compare matches1 matches2

        list |> List.sortWith compareNatural

    // Parallel Merge Pipeline
    let mergeFiles (filePaths: string list) (outputPath: string) =
        let sortedFiles = naturalSort filePaths
        PdfWriter.mergeFiles (List.toArray sortedFiles) outputPath

    let mergeFilesEnumerable (filePaths: System.Collections.Generic.IEnumerable<string>) (outputPath: string) =
        mergeFiles (Seq.toList filePaths) outputPath

module SignatureWorkflow =
    let signPdf (inputPath: string) (certificate: byte[]) = async {
        // Step 1: Calculate Hash using Native Engine
        // Step 2: External Signature service call (if any)
        // Step 3: Apply Signature using Native Engine
        return "Signed!"
    }
