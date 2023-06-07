using JsonCSharp;
using JsonCSharp.Index.Hero;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

public class Program
{
    static List<ContentHierarchy> contentHierarchies = new List<ContentHierarchy>();
    static void Main(string[] args)
    {
        PageData pageData = new PageData();
        string jsonContent = File.ReadAllText("D:\\Git Projects\\JsonCSharp\\Json_2.json");
        var res = JsonDocument.Parse(jsonContent);


        JsonElement rss = res.RootElement.GetProperty("data").GetProperty("page").GetProperty("rawContent").GetProperty("data");

        List<Region> lstRegions = new List<Region>();

        var result = DirSearch("D:\\Git Projects\\JsonCSharp\\FAF\\Index");

        foreach (ContentHierarchy item in contentHierarchies)
        {
            Region region = new Region() { Name=item.ContainerName};   

            if (item.ContentType == "Entity")
            {

                ClassInfo classInfo = ParseCSharpFile("D:\\Git Projects\\JsonCSharp\\FAF\\Index\\" + item.ContainerName + "\\" + item.ContentName);
                Type entity = Type.GetType(classInfo.Namespace + "." + classInfo.ClassName);

                JsonElement entityFound;
                var r = rss.GetProperty("Regions").EnumerateArray()
                    .Where(p => p.GetProperty("Name").ToString() == item.ContainerName)
                    .Select(p => p)
                    .FirstOrDefault();

                r.TryGetProperty("Entities", out entityFound);

                IEnumerable<JsonElement> myEntityName = entityFound.EnumerateArray()
                    .Where(p => p.GetProperty("ComponentTemplate").GetProperty("Title").ToString() == classInfo.ClassName);


                if (myEntityName.Any())
                {
                    JsonElement content = default;
                    JsonElement itemListElement = default;
                    JsonElement _type = default;
                    JsonElement _values = default;

                    myEntityName
                        .FirstOrDefault()
                        .TryGetProperty("Content", out content);


                    if (content.ValueKind != JsonValueKind.Undefined)
                    {
                        itemListElement = content.GetProperty("itemListElement");

                        if (itemListElement.ValueKind != JsonValueKind.Undefined)
                        {
                            itemListElement.TryGetProperty("$type", out _type);

                            if (_type.ValueKind != JsonValueKind.Undefined)
                            {
                                itemListElement.TryGetProperty("$values", out _values);

                                if (_values.ValueKind != JsonValueKind.Undefined)
                                {
                                    List<object> lst = new List<object>();

                                    if (Convert.ToString(_type) == "ContentModelData[]")
                                    {
                                        foreach (var a in _values.EnumerateArray())
                                        {

                                            List<dynamic> lstDymanic = new List<dynamic>();
                                            object currentObjectEntity = Activator.CreateInstance(entity);

                                            foreach (PropertyInfo propertyInfo in entity.GetProperties())
                                            {
                                                var val = a.EnumerateObject()
                                                    .Where(p => p.Name == propertyInfo.Name)
                                                    .FirstOrDefault()
                                                    .Value
                                                    .ToString();

                                                currentObjectEntity
                                                    .GetType()
                                                    .GetProperty(propertyInfo.Name)
                                                    .SetValue(currentObjectEntity, val, null);
                                            }
                                            lst.Add(currentObjectEntity);

                                        }
                                    }

                                    region.Entities = lst;
                                    lstRegions.Add(region);
                                }
                            }
                        }

                    }
                    
                }


            }
        }

        pageData.Regions = lstRegions;
        Console.WriteLine(JsonSerializer.Serialize(pageData, new JsonSerializerOptions() { WriteIndented = true }));
        Console.ReadKey();
    }
    public static ClassInfo ParseCSharpFile(string filePath)
    {
        // Read the C# file content
        string code = File.ReadAllText(filePath);

        // Create a Roslyn syntax tree from the file content
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);

        // Get the root of the syntax tree
        CompilationUnitSyntax root = syntaxTree.GetCompilationUnitRoot();

        // Retrieve the namespace and class declarat        ion nodes
        NamespaceDeclarationSyntax namespaceDeclaration = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
        ClassDeclarationSyntax classDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();

        if (namespaceDeclaration != null && classDeclaration != null)
        {
            // Extract the namespace and class names
            string namespaceName = namespaceDeclaration.Name.ToString();
            string className = classDeclaration.Identifier.ToString();

            return new ClassInfo
            {
                Namespace = namespaceName,
                ClassName = className
            };
        }

        // If the namespace or class is not found, return null
        return null;
    }
    private static List<ContentHierarchy> DirSearch(string dir)
    {

        string parDir = dir.Split('\\').Last();
        string typeName = string.Empty;

        try
        {
            foreach (string f in Directory.GetFiles(dir))
            {

                typeName = f.Split('\\').Last();
                contentHierarchies.Add(new ContentHierarchy()
                {
                    ContentType = "Entity",
                    ContainerName = parDir,
                    ContentName = typeName
                }); ;
                //Console.WriteLine(f);
            }
            foreach (string d in Directory.GetDirectories(dir))
            {
                typeName = d.Split('\\').Last();
                //Console.WriteLine(typeName);
                contentHierarchies.Add(new ContentHierarchy()
                {
                    ContentType = "Region",
                    ContainerName = parDir,
                    ContentName = typeName
                }); ;
                DirSearch(d);
            }

        }
        catch (System.Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        return contentHierarchies;
    }
}
public class ClassInfo
{
    public string Namespace { get; set; }
    public string ClassName { get; set; }
}