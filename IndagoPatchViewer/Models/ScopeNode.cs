using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Indago.DataTypes;

namespace IndagoPatchViewer.Models;

public class ScopeNode(Scope scope)
{
    public List<ScopeNode> SubScopes { get; } = new ();

    public Scope Scope => scope;

    public ScopeNode GetChildByName(string name) => SubScopes.First(s => s.Name == name);

    public ScopeNode GetChildByPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return this;
        if (!path.Contains('.')) return GetChildByName(path);
        
        string[] pathParts = path.Split('.');
        string childOfMe = pathParts[0];
        string restOfPath = string.Join('.', pathParts[1..]);
        return GetChildByName(childOfMe).GetChildByPath(restOfPath);
    }

    public static string RemoveParentPath(string path) => !path.Contains('.') ? "" : 
        path[(path.IndexOf('.') + 1)..];

    public string Name => scope.Name;

    public string Path => scope.Path;

    public string Parent => string.IsNullOrWhiteSpace(Path) ? "" : Path.Split('.')[^1];

    public override string ToString() => Name;
}