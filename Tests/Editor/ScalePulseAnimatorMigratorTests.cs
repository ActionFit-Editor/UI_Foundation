using NUnit.Framework;

namespace ActionFit.UIFoundation.Editor.Tests
{
    public sealed class ScalePulseAnimatorMigratorTests
    {
        private const string TargetControllerGuid = "11111111111111111111111111111111";
        private const string OtherControllerGuid = "22222222222222222222222222222222";
        private const string TargetPrefabGuid = "33333333333333333333333333333333";
        private const string OtherPrefabGuid = "44444444444444444444444444444444";
        private const string ScalePulseScriptGuid = "f306b52898894266aae652768dc67b62";

        [Test]
        public void RewritePreservesComponentFileIdAndUnrelatedAnimator()
        {
            string yaml = BuildYaml();

            string rewritten = ScalePulseAnimatorMigrator.RewriteYamlForTests(
                yaml,
                new[] { TargetControllerGuid },
                ScalePulseScriptGuid,
                out int replacementCount);

            Assert.That(replacementCount, Is.EqualTo(1));
            Assert.That(rewritten, Does.Contain("--- !u!114 &42\nMonoBehaviour:"));
            Assert.That(rewritten, Does.Contain("- component: {fileID: 42}"));
            Assert.That(rewritten, Does.Contain("m_GameObject: {fileID: 10}"));
            Assert.That(rewritten, Does.Contain($"m_Script: {{fileID: 11500000, guid: {ScalePulseScriptGuid}, type: 3}}"));
            Assert.That(rewritten, Does.Not.Contain(TargetControllerGuid));
            Assert.That(rewritten, Does.Contain(OtherControllerGuid));
            Assert.That(rewritten, Does.Contain("--- !u!95 &84\nAnimator:"));
        }

        [Test]
        public void RewriteIsIdempotent()
        {
            string first = ScalePulseAnimatorMigrator.RewriteYamlForTests(
                BuildYaml(),
                new[] { TargetControllerGuid },
                ScalePulseScriptGuid,
                out int firstCount);

            string second = ScalePulseAnimatorMigrator.RewriteYamlForTests(
                first,
                new[] { TargetControllerGuid },
                ScalePulseScriptGuid,
                out int secondCount);

            Assert.That(firstCount, Is.EqualTo(1));
            Assert.That(secondCount, Is.Zero);
            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void RewriteConvertsOnlyExactStrippedPrefabComponentReference()
        {
            string yaml = BuildStrippedYaml();
            var target = new ScalePulsePrefabComponentReference(TargetPrefabGuid, 6351428021986149140);

            string rewritten = ScalePulseAnimatorMigrator.RewriteYamlForTests(
                yaml,
                new[] { TargetControllerGuid },
                new[] { target },
                ScalePulseScriptGuid,
                out int replacementCount);

            Assert.That(replacementCount, Is.EqualTo(1));
            Assert.That(rewritten, Does.Contain("--- !u!114 &42 stripped\nMonoBehaviour:"));
            Assert.That(rewritten, Does.Contain(
                $"m_CorrespondingSourceObject: {{fileID: 6351428021986149140, guid: {TargetPrefabGuid}, type: 3}}"));
            Assert.That(rewritten, Does.Contain("--- !u!95 &84 stripped\nAnimator:"));
            Assert.That(rewritten, Does.Contain(OtherPrefabGuid));

            string second = ScalePulseAnimatorMigrator.RewriteYamlForTests(
                rewritten,
                new[] { TargetControllerGuid },
                new[] { target },
                ScalePulseScriptGuid,
                out int secondCount);

            Assert.That(secondCount, Is.Zero);
            Assert.That(second, Is.EqualTo(rewritten));
        }

        [Test]
        public void LavaRushPrefixesAreExcludedWithOrdinalMatching()
        {
            string[] excluded =
            {
                "Assets/_Project/Content/LavaRush/",
                "Packages/com.actionfit.lava-rush."
            };

            Assert.That(
                ScalePulseAnimatorMigrator.IsExcludedForTests(
                    "Assets/_Project/Content/LavaRush/Prefabs/Icon.prefab",
                    excluded),
                Is.True);
            Assert.That(
                ScalePulseAnimatorMigrator.IsExcludedForTests(
                    "Packages/com.actionfit.lava-rush.ui/Runtime/Icon.prefab",
                    excluded),
                Is.True);
            Assert.That(
                ScalePulseAnimatorMigrator.IsExcludedForTests(
                    "Packages/com.actionfit.match-rival.ui/Runtime/Icon.prefab",
                    excluded),
                Is.False);
        }

        private static string BuildYaml()
        {
            return "%YAML 1.1\n"
                + "--- !u!1 &10\n"
                + "GameObject:\n"
                + "  m_Component:\n"
                + "  - component: {fileID: 42}\n"
                + "  - component: {fileID: 84}\n"
                + "--- !u!95 &42\n"
                + "Animator:\n"
                + "  serializedVersion: 7\n"
                + "  m_ObjectHideFlags: 0\n"
                + "  m_CorrespondingSourceObject: {fileID: 0}\n"
                + "  m_PrefabInstance: {fileID: 0}\n"
                + "  m_PrefabAsset: {fileID: 0}\n"
                + "  m_GameObject: {fileID: 10}\n"
                + "  m_Enabled: 1\n"
                + $"  m_Controller: {{fileID: 9100000, guid: {TargetControllerGuid}, type: 2}}\n"
                + "--- !u!95 &84\n"
                + "Animator:\n"
                + "  serializedVersion: 7\n"
                + "  m_ObjectHideFlags: 0\n"
                + "  m_CorrespondingSourceObject: {fileID: 0}\n"
                + "  m_PrefabInstance: {fileID: 0}\n"
                + "  m_PrefabAsset: {fileID: 0}\n"
                + "  m_GameObject: {fileID: 10}\n"
                + "  m_Enabled: 1\n"
                + $"  m_Controller: {{fileID: 9100000, guid: {OtherControllerGuid}, type: 2}}\n";
        }

        private static string BuildStrippedYaml()
        {
            return "%YAML 1.1\n"
                + "--- !u!95 &42 stripped\n"
                + "Animator:\n"
                + $"  m_CorrespondingSourceObject: {{fileID: 6351428021986149140, guid: {TargetPrefabGuid}, type: 3}}\n"
                + "  m_PrefabInstance: {fileID: 10}\n"
                + "  m_PrefabAsset: {fileID: 0}\n"
                + "--- !u!95 &84 stripped\n"
                + "Animator:\n"
                + $"  m_CorrespondingSourceObject: {{fileID: 6351428021986149140, guid: {OtherPrefabGuid}, type: 3}}\n"
                + "  m_PrefabInstance: {fileID: 10}\n"
                + "  m_PrefabAsset: {fileID: 0}\n";
        }
    }
}
