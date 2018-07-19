module internal Seq

let count predicate source =
    source |> Seq.fold (fun acc it -> if predicate it then acc + 1 else acc) 0

let any source = source |> Seq.isEmpty |> not
