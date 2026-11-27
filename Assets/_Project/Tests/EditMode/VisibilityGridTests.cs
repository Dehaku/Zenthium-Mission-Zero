using NUnit.Framework;
using UnityEngine;
using Zenthium.World;

namespace Zenthium.Tests.EditMode
{
    /// <summary>
    /// The fog of war (maps.md, Visibility), on a 10 x 10 m map with 1 m cells: the player's sight makes cells
    /// Visible, cells left behind stay Explored, and the map starts unexplored.
    /// </summary>
    public class VisibilityGridTests
    {
        private static VisibilityGrid Grid()
        {
            return new VisibilityGrid(new Rect(0f, 0f, 10f, 10f), 1f);
        }

        private static void Update(VisibilityGrid grid, Vector2 origin, Vector2 direction, float distance)
        {
            grid.BeginUpdate();
            grid.RevealRay(origin, direction, distance);
            grid.EndUpdate();
        }

        [Test]
        public void New_EveryCellIsUnexplored()
        {
            VisibilityGrid grid = Grid();

            Assert.That(grid.StateAt(new Vector2(0.5f, 0.5f)), Is.EqualTo(FogState.Unexplored));
            Assert.That(grid.StateAt(new Vector2(9.5f, 9.5f)), Is.EqualTo(FogState.Unexplored));
        }

        [TestCase(10f, 10f, 1f, 10, 10)]
        [TestCase(10.5f, 4f, 2f, 6, 2)]
        public void New_CoversTheWholeArea(float width, float height, float cellSize, int columns, int rows)
        {
            var grid = new VisibilityGrid(new Rect(0f, 0f, width, height), cellSize);

            Assert.That(grid.Columns, Is.EqualTo(columns));
            Assert.That(grid.Rows, Is.EqualTo(rows));
        }

        [Test]
        public void RevealRay_MakesCellsVisibleUpToTheDistance()
        {
            VisibilityGrid grid = Grid();

            Update(grid, new Vector2(0.5f, 0.5f), Vector2.right, 3f);

            for (int column = 0; column <= 3; column++)
            {
                Assert.That(grid[column, 0], Is.EqualTo(FogState.Visible), $"Column {column}");
            }

            Assert.That(grid[4, 0], Is.EqualTo(FogState.Unexplored));
            Assert.That(grid[0, 1], Is.EqualTo(FogState.Unexplored));
        }

        [Test]
        public void NextUpdate_CellsNoLongerInSightAreExplored()
        {
            VisibilityGrid grid = Grid();
            Update(grid, new Vector2(0.5f, 0.5f), Vector2.right, 3f);

            Update(grid, new Vector2(0.5f, 0.5f), Vector2.right, 1f);

            Assert.That(grid[1, 0], Is.EqualTo(FogState.Visible));
            Assert.That(grid[2, 0], Is.EqualTo(FogState.Explored));
            Assert.That(grid[3, 0], Is.EqualTo(FogState.Explored));
            Assert.That(grid.IsVisible(new Vector2(3.5f, 0.5f)), Is.False);
        }

        [Test]
        public void Version_ChangesOnlyWhenACellDoes()
        {
            VisibilityGrid grid = Grid();
            Update(grid, new Vector2(0.5f, 0.5f), Vector2.up, 4f);
            int afterFirst = grid.Version;

            Update(grid, new Vector2(0.5f, 0.5f), Vector2.up, 4f);
            int afterSame = grid.Version;
            Update(grid, new Vector2(0.5f, 0.5f), Vector2.up, 2f);

            Assert.That(afterFirst, Is.Not.EqualTo(0));
            Assert.That(afterSame, Is.EqualTo(afterFirst));
            Assert.That(grid.Version, Is.Not.EqualTo(afterSame));
        }

        [Test]
        public void RevealRay_OffTheMap_MarksOnlyCellsOnIt()
        {
            VisibilityGrid grid = Grid();

            Update(grid, new Vector2(-5f, 0.5f), Vector2.right, 6.5f);

            Assert.That(grid[0, 0], Is.EqualTo(FogState.Visible));
            Assert.That(grid[1, 0], Is.EqualTo(FogState.Visible));
            Assert.That(grid[2, 0], Is.EqualTo(FogState.Unexplored));
            Assert.That(grid.StateAt(new Vector2(-1f, 0.5f)), Is.EqualTo(FogState.Unexplored));
        }

        [Test]
        public void RevealAll_ExploresEverythingAndKeepsSight()
        {
            VisibilityGrid grid = Grid();
            Update(grid, new Vector2(0.5f, 0.5f), Vector2.right, 1f);

            grid.RevealAll();

            Assert.That(grid[0, 0], Is.EqualTo(FogState.Visible));
            Assert.That(grid[9, 9], Is.EqualTo(FogState.Explored));
            Assert.That(grid[5, 3], Is.EqualTo(FogState.Explored));
        }
    }
}
