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

    let private speciesRadios (species: Var<Species>) =
        Doc.Concat [
            speciesRadio species Cat
            speciesRadio species Dog
            speciesRadio species Piglet
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

    [<SPAEntryPoint>]
    let Main () =
        let submitted = Var.Create (None: string option)

        let renderPerson
            (firstName: Var<string>)
            (lastName: Var<string>)
            (pets: Form.Many.CollectionWithDefault<Pet, _, _>)
            (submit: Submitter<Result<Person>>)
            =
            Template.Person()
                .Banner(submitted.View |> View.Map (fun m -> defaultArg m ""))
                .FirstName(firstName)
                .FirstNameErrors(showErrors (submit.View.Through firstName))
                .LastName(lastName)
                .LastNameErrors(showErrors (submit.View.Through lastName))
                .Pets(
                    pets.Render (fun (ops: Form.Many.ItemOperations) (species: Var<Species>) (name: Var<string>) ->
                        Template.Pet()
                            .Species(speciesRadios species)
                            .Name(name)
                            .NameErrors(showErrors (submit.View.Through name))
                            .MoveUpAttr(WebSharper.Forms.Attr.SubmitterValidate ops.MoveUp)
                            .MoveDownAttr(WebSharper.Forms.Attr.SubmitterValidate ops.MoveDown)
                            .Delete(fun _ -> ops.Delete())
                            .Doc()))
                .AddPet(fun _ -> pets.Add())
                .Submit(fun _ -> submit.Trigger())
                .Doc()

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
