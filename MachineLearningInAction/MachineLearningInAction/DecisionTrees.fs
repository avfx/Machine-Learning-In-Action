﻿namespace MachineLearning

type Tree = 
    | Conclusion of string 
    | Choice of string * (string * Tree) []

module DecisionTrees =

    let testData =
        [| "Color"; "Size"; "Class" |],
        [| [| "Red";   "Small"; "Alpha" |];
           [| "Red";   "Small"; "Alpha" |];
           [| "Red";   "Big";   "Bravo"  |];
           [| "Green"; "Small"; "Bravo"  |];
           [| "Green"; "Small"; "Bravo"  |] |]

    let prop count total = (float)count / (float)total

    let inspect dataset =
        let header, (data: 'a [][]) = dataset
        let rows = data |> Array.length
        let columns = header |> Array.length
        header, data, rows, columns

    let h vector =
        let size = vector |> Array.length
        vector 
        |> Seq.groupBy (fun e -> e)
        |> Seq.sumBy (fun e ->
            let count = e |> snd |> Seq.length
            let p = prop count size
            - p * log p)

    let shannonEntropy dataset =
        let hdr, data, rows, cols = inspect dataset
        data
        |> Seq.map (fun row -> row.[cols-1])
        |> Seq.toArray
        |> h

    let remove i vector =
        let size = vector |> Array.length
        Array.append vector.[ 0 .. i-1 ] vector.[ i+1 .. size-1 ]

    let split dataset i =
        let hdr, data, rows, cols = inspect dataset
        remove i hdr,
        data
        |> Seq.groupBy (fun row -> row.[i])
        |> Seq.map (fun (label, group) -> 
            label,
            group |> Seq.toArray |> Array.map (remove i))

    let splitEntropy dataset i =
        let hdr, data, rows, cols = inspect dataset
        data
        |> Seq.groupBy(fun row -> row.[i])
        |> Seq.map (fun (label, group) -> 
            group 
            |> Seq.map (fun row -> row.[cols - 1]) 
            |> Seq.toArray)
        |> Seq.sumBy (fun subset -> 
            let p = prop (Array.length subset) rows
            p * h subset)

    let selectSplit dataset =
        let hdr, data, rows, cols = inspect dataset
        if cols < 2 
        then None
        else
            let currentEntropy = shannonEntropy dataset      
            let feature =
                hdr.[0 .. cols - 2]
                |> Array.mapi (fun i f ->
                    (i, f), currentEntropy - splitEntropy dataset i)
                |> Array.maxBy (fun f -> snd f)
            if (snd feature > 0.0) then Some(fst feature) else None

    let majority dataset =
        let hdr, data, rows, cols = inspect dataset
        data
        |> Seq.groupBy (fun row -> row.[cols-1])
        |> Seq.maxBy (fun (label, group) -> Seq.length group)
        |> fst

    let rec classify subject tree =
        match tree with
        | Conclusion(c) -> c
        | Choice(label, options) ->
            let subjectState =
                subject
                |> Seq.find(fun o -> (fst o) = label)
                |> snd
            options
            |> Array.find (fun t -> (fst t) = subjectState)
            |> snd
            |> classify subject

    let rec build dataset =
        match selectSplit dataset with
        | None -> Conclusion(majority dataset)
        | Some(feature) -> 
            let (index, name) = feature
            let (header, groups) = split dataset index
            let trees = 
                groups 
                |> Seq.map (fun (label, data) -> (label, build (header, data)))
                |> Seq.toArray
            Choice(name, trees)
        