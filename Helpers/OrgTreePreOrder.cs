namespace HcPortal.Helpers
{
    public static class OrgTreePreOrder
    {
        /// <summary>
        /// Pre-order DFS flatten of an org-unit tree (ORG-TREE-01 / TEST-03).
        /// Mirrors wwwroot/js/orgTree.js buildTree + flattenTreePreOrder EXACTLY:
        /// NO internal re-ordering — consumes the flat input in insertion order, just like the JS
        /// consumes _flatUnits. CALLER CONTRACT: input MUST already be ordered like the endpoint
        /// (OrganizationController.GetOrganizationTree: Level, DisplayOrder, Name).
        /// Roots = ParentId == null, each emitted then immediately followed by ALL descendants
        /// (children in the order they appear in the input array).
        /// </summary>
        public static List<(int Id, int Depth)> BuildPreOrder(
            IReadOnlyList<(int Id, int? ParentId, int DisplayOrder, string Name)> flat)
        {
            // Partition into roots + a children lookup keyed by NON-NULL parent id, preserving the
            // input array's insertion order (mirrors JS buildTree which pushes children in flatList
            // iteration order). A null ParentId cannot be a dictionary key, so roots are kept in a
            // separate list rather than grouped on a nullable key. No re-ordering is applied here.
            var roots = new List<(int Id, int? ParentId, int DisplayOrder, string Name)>();
            var childrenByParent = new Dictionary<int, List<(int Id, int? ParentId, int DisplayOrder, string Name)>>();
            foreach (var n in flat)
            {
                if (n.ParentId is null)
                {
                    roots.Add(n);
                }
                else
                {
                    if (!childrenByParent.TryGetValue(n.ParentId.Value, out var kids))
                    {
                        kids = new List<(int Id, int? ParentId, int DisplayOrder, string Name)>();
                        childrenByParent[n.ParentId.Value] = kids;
                    }
                    kids.Add(n);
                }
            }

            var outList = new List<(int, int)>();
            void Walk((int Id, int? ParentId, int DisplayOrder, string Name) node, int depth)
            {
                outList.Add((node.Id, depth));
                if (childrenByParent.TryGetValue(node.Id, out var kids))
                {
                    foreach (var k in kids) Walk(k, depth + 1);
                }
            }
            foreach (var r in roots) Walk(r, 0);
            return outList;
        }
    }
}
