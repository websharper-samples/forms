namespace Samples

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Html
open WebSharper.UI.Templating
open WebSharper.UI.Client
open WebSharper.Forms

/// A reactive, validated form built with WebSharper.Forms (formlets):
/// enter your details, add and reorder pets, and submit.
[<JavaScript>]
module PersonPets =

    type Template = Templating.Template<"wwwroot/index.html", ClientLoad.FromDocument>

    type Species =
        | Cat
        | Dog
        | Piglet

        override this.ToString() =
            match this with
            | Cat -> "cat"
            | Dog -> "dog"
            | Piglet -> "piglet"

    type Pet =
        {
            Species: Species
            Name: string
        }

    type Person =
        {
            FirstName: string
            LastName: string
            Pets: seq<Pet>
        }

    let PetPiglet (init: Pet) =
        Form.Return (fun species name -> { Species = species; Name = name })
        <*> Form.Yield init.Species
        <*> (Form.Yield init.Name
             |> Validation.IsNotEmpty "Please enter your pet's name.")

    let PersonPiglet (init: Person) =
        Form.Return (fun first last pets ->
            { FirstName = first; LastName = last; Pets = pets })
        <*> (Form.Yield init.FirstName
             |> Validation.IsNotEmpty "Please enter your first name.")
        <*> (Form.Yield init.LastName
             |> Validation.IsNotEmpty "Please enter your last name.")
        <*> Form.Many init.Pets { Species = Cat; Name = "" } PetPiglet
        |> Form.WithSubmit

    let private speciesRadio (species: Var<Species>) value =
        label [ attr.``class`` "radio" ] [
            Doc.InputType.Radio [] value species
            text (string value)
        ]

    let private renderPet (species: Var<Species>) (name: Var<string>) =
        div [ attr.``class`` "pet" ] [
            div [ attr.``class`` "radio-group" ] [
                speciesRadio species Cat
                speciesRadio species Dog
                speciesRadio species Piglet
            ]
            Doc.InputType.Text [ attr.``class`` "input"; attr.placeholder "Pet name" ] name
        ]

    let private showErrors (v: View<Result<'T>>) =
        v
        |> View.Map (function
            | Success _ -> Doc.Empty
            | Failure errors ->
                Doc.Concat [
                    for error in errors do
                        yield div [ attr.``class`` "error" ] [ text error.Text ]
                ])
        |> Doc.EmbedView

    let private field caption child errors =
        div [ attr.``class`` "field" ] [
            label [ attr.``class`` "field-label" ] [ text caption ]
            child
            errors
        ]

    [<SPAEntryPoint>]
    let Main () =
        let submitted = Var.Create (None: string option)

        let renderPerson
            (firstName: Var<string>)
            (lastName: Var<string>)
            (pets: Form.Many.CollectionWithDefault<Pet, _, _>)
            (submit: Submitter<Result<Person>>)
            =
            Doc.Concat [
                div [
                    attr.``class`` "banner-slot"
                    on.viewUpdate submitted.View (fun el msg ->
                        el.TextContent <- defaultArg msg "")
                ] []

                field "First name"
                    (Doc.InputType.Text [ attr.``class`` "input" ] firstName)
                    (showErrors (submit.View.Through firstName))

                field "Last name"
                    (Doc.InputType.Text [ attr.``class`` "input" ] lastName)
                    (showErrors (submit.View.Through lastName))

                div [ attr.``class`` "section-title" ] [
                    text "Your pets"
                    Doc.Button "Add a pet" [ attr.``class`` "btn btn-ghost btn-small" ] pets.Add
                ]

                pets.Render (fun ops species name ->
                    div [ attr.``class`` "pet-row" ] [
                        renderPet species name
                        div [ attr.``class`` "pet-actions" ] [
                            Doc.ButtonValidate "Up" [ attr.``class`` "btn-icon" ] ops.MoveUp
                            Doc.ButtonValidate "Down" [ attr.``class`` "btn-icon" ] ops.MoveDown
                            Doc.Button "Delete" [ attr.``class`` "btn-icon btn-danger" ] ops.Delete
                        ]
                        showErrors (submit.View.Through name)
                    ])

                div [ attr.``class`` "actions" ] [
                    Doc.Button "Submit" [ attr.``class`` "btn btn-primary" ] submit.Trigger
                ]
            ]

        PersonPiglet {
            FirstName = ""
            LastName = ""
            Pets = [||]
        }
        |> Form.Run (fun p ->
            submitted.Value <-
                Some (
                    "Welcome, " + p.FirstName + " " + p.LastName + "!" +
                    (p.Pets
                     |> Seq.map (fun pet -> ", your " + string pet.Species + " " + pet.Name)
                     |> String.concat "")))
        |> Form.Render renderPerson
        |> fun form -> Template().Form(form).Bind()
