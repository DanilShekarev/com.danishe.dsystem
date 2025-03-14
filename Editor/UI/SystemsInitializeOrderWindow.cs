using System.Collections.Generic;
using System.Reflection;
using DSystem;
using DSystem.Utils.Tree;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.UI
{
    public class SystemsInitializeOrderWindow : EditorWindow
    {
        [MenuItem("DSystem/Initialize Order Debug")]
        private static void ShowWindow()
        {
            var window = GetWindow<SystemsInitializeOrderWindow>();
            window.titleContent = new GUIContent("Systems Initialize Order Debug");
            window.Show();
        }
        
        private MultiColumnTreeView _treeView;

        private void CreateGUI()
        {
            CreateTreeView();
            rootVisualElement.Add(_treeView);
        }
        
        private void CreateTreeView()
        {
            var items = GetItems();
            
            _treeView = new MultiColumnTreeView()
            {
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
            };
            _treeView.columns.Add(new Column()
            {
                title = "Name", 
                makeCell = MakeLabel, 
                bindCell = BindName,
                width = 150,
            });
            _treeView.columns.Add(new Column()
            {
                title = "Order", 
                makeCell = MakeLabel, 
                bindCell = BindOrder,
                width = 50,
            });
            _treeView.columns.Add(new Column()
            {
                title = "User Order", 
                makeCell = MakeLabel, 
                bindCell = BindUserOrder,
                width = 50,
            });
            _treeView.SetRootItems(items);
        }
        
        private VisualElement MakeLabel()
        {
            return new Label();
        }

        private void BindName(VisualElement element, int index)
        {
            var item = _treeView.GetItemDataForIndex<CustomElement>(index);
            element.Q<Label>().text = item.Name;
        }

        private void BindOrder(VisualElement element, int index)
        {
            var item = _treeView.GetItemDataForIndex<CustomElement>(index);
            element.Q<Label>().text = item.Order.ToString();
        }
        
        private void BindUserOrder(VisualElement element, int index)
        {
            var item = _treeView.GetItemDataForIndex<CustomElement>(index);
            element.Q<Label>().text = item.UserOrder.ToString();
        }

        private List<TreeViewItemData<CustomElement>> GetItems()
        {
            var items = new List<TreeViewItemData<CustomElement>>();
            
            int index = 0;
            int order = 0;
            RecGetItem(items, DEntry.InjectorDebugger.Tree.RootNode, ref order, ref index);
            
            return items;
        }
        
        private void RecGetItem(List<TreeViewItemData<CustomElement>> list, Node<object> node, ref int order, ref int index)
        {
            List<TreeViewItemData<CustomElement>> childList = null;
            if (node.Data == null)
                childList = list;
            else if (node.Children.Count > 0)
                childList = new ();
            
            var tempIndex = index;
            if (node.Data != null)
                index++;
            
            foreach (var child in node.Children)
            {
                RecGetItem(childList, child, ref order, ref index);
            }
            
            if (node.Data != null)
            {
                var newElement = new CustomElement()
                {
                    Name = node.Data.GetType().Name,
                    Order = order,
                };
                var registryAttr = node.Data.GetType().GetCustomAttribute<AutoRegistryAttribute>();
                if (registryAttr != null)
                {
                    newElement.UserOrder = registryAttr.Order;
                }
                list.Add(new TreeViewItemData<CustomElement>(tempIndex, newElement, childList));
                order++;
            }
        }
        
        private class CustomElement
        {
            public int Order { get; set; }
            public int UserOrder { get; set; }
            public string Name { get; set; }
        }
    }
}