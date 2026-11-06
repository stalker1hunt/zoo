using System;
using NUnit.Framework;

namespace ZooWorld.Tests
{
    public class AnimalDatabaseTests
    {
        private AnimalTestData data;

        [SetUp] public void SetUp() => data = new AnimalTestData();
        [TearDown] public void TearDown() => data.Dispose();

        [TestCase(AnimalRole.Prey, AnimalRole.Prey, AnimalInteraction.Bounce)]
        [TestCase(AnimalRole.Predator, AnimalRole.Prey, AnimalInteraction.FirstEatsSecond)]
        [TestCase(AnimalRole.Prey, AnimalRole.Predator, AnimalInteraction.SecondEatsFirst)]
        [TestCase(AnimalRole.Predator, AnimalRole.Predator, AnimalInteraction.OneEatsOther)]
        public void DefaultInteractionsFollowFoodChain(AnimalRole firstRole, AnimalRole secondRole, AnimalInteraction expected)
        {
            AnimalDefinition first = data.Species("First", firstRole);
            AnimalDefinition second = data.Species("Second", secondRole);
            Assert.That(data.Database(first, second).GetInteraction(first, second), Is.EqualTo(expected));
        }

        [TestCase(AnimalInteraction.FirstEatsSecond, AnimalInteraction.SecondEatsFirst)]
        [TestCase(AnimalInteraction.SecondEatsFirst, AnimalInteraction.FirstEatsSecond)]
        public void DirectionalExceptionPreservesEaterWhenQueryIsReversed(AnimalInteraction authored, AnimalInteraction reversed)
        {
            AnimalDefinition first = data.Species("First", AnimalRole.Prey);
            AnimalDefinition second = data.Species("Second", AnimalRole.Prey);
            AnimalDatabase database = data.Database(first, second);
            AnimalTestData.SetRules(database, (first, second, authored));
            Assert.That(database.GetInteraction(first, second), Is.EqualTo(authored));
            Assert.That(database.GetInteraction(second, first), Is.EqualTo(reversed));
        }

        [TestCase(AnimalInteraction.Bounce)]
        [TestCase(AnimalInteraction.PhysicalContact)]
        [TestCase(AnimalInteraction.OneEatsOther)]
        public void SymmetricExceptionsWorkForSameSpecies(AnimalInteraction interaction)
        {
            AnimalDefinition species = data.Species("Species", AnimalRole.Prey);
            AnimalDatabase database = data.Database(species);
            AnimalTestData.SetRules(database, (species, species, interaction));
            Assert.That(database.GetInteraction(species, species), Is.EqualTo(interaction));
        }

        [Test]
        public void ReverseDuplicatePairIsRejectedInsteadOfDependingOnRuleOrder()
        {
            AnimalDefinition first = data.Species("First", AnimalRole.Prey);
            AnimalDefinition second = data.Species("Second", AnimalRole.Predator);
            AnimalDatabase database = data.Database(first, second);
            AnimalTestData.SetRules(database, (first, second, AnimalInteraction.Bounce),
                (second, first, AnimalInteraction.PhysicalContact));
            Assert.Throws<InvalidOperationException>(() => database.ValidateConfiguration());
        }

        [TestCase(AnimalInteraction.FirstEatsSecond)]
        [TestCase(AnimalInteraction.SecondEatsFirst)]
        public void SameSpeciesCannotHaveAnAuthoredDirectionalWinner(AnimalInteraction interaction)
        {
            AnimalDefinition species = data.Species("Species", AnimalRole.Prey);
            AnimalDatabase database = data.Database(species);
            AnimalTestData.SetRules(database, (species, species, interaction));
            Assert.Throws<InvalidOperationException>(() => database.ValidateConfiguration());
        }

        [Test]
        public void ExceptionCannotReferenceAnUnregisteredSpecies()
        {
            AnimalDefinition registered = data.Species("Registered", AnimalRole.Prey);
            AnimalDefinition outside = data.Species("Outside", AnimalRole.Prey);
            AnimalDatabase database = data.Database(registered);
            AnimalTestData.SetRules(database, (registered, outside, AnimalInteraction.Bounce));
            Assert.Throws<InvalidOperationException>(() => database.ValidateConfiguration());
        }

        [Test]
        public void DuplicateSpeciesCannotBiasRandomSelection()
        {
            AnimalDefinition species = data.Species("Species", AnimalRole.Prey);
            Assert.Throws<InvalidOperationException>(() => data.Database(species, species).ValidateConfiguration());
        }

        [Test]
        public void SpeciesNamesAreUniqueIgnoringCase()
        {
            AnimalDefinition first = data.Species("Frog", AnimalRole.Prey);
            AnimalDefinition second = data.Species("FROG", AnimalRole.Predator);
            Assert.Throws<InvalidOperationException>(() => data.Database(first, second).ValidateConfiguration());
        }

        [Test]
        public void EmptyDatabaseCannotStartASpawnSession()
        {
            Assert.Throws<InvalidOperationException>(() => data.Database().ValidateConfiguration());
        }

        [Test]
        public void ThousandSpeciesReuseExistingMovementWithoutPairMatrix()
        {
            var species = new AnimalDefinition[1000];
            for (int i = 0; i < species.Length; i++)
                species[i] = data.Species($"Species {i}", i % 2 == 0 ? AnimalRole.Prey : AnimalRole.Predator);
            AnimalDatabase database = data.Database(species);
            AnimalTestData.SetRules(database, (species[0], species[999], AnimalInteraction.PhysicalContact));

            database.ValidateConfiguration();

            Assert.That(database.Definitions.Count, Is.EqualTo(1000));
            Assert.That(database.GetInteraction(species[0], species[999]), Is.EqualTo(AnimalInteraction.PhysicalContact));
            Assert.That(database.GetInteraction(species[999], species[0]), Is.EqualTo(AnimalInteraction.PhysicalContact));
            Assert.That(database.GetInteraction(species[500], species[501]), Is.EqualTo(AnimalInteraction.SecondEatsFirst));
            Assert.That(database.GetInteraction(species[501], species[503]), Is.EqualTo(AnimalInteraction.OneEatsOther));
        }

        [Test]
        public void InspectorRuleEditInvalidatesPreviouslyQueriedResult()
        {
            AnimalDefinition first = data.Species("First", AnimalRole.Prey);
            AnimalDefinition second = data.Species("Second", AnimalRole.Predator);
            AnimalDatabase database = data.Database(first, second);
            Assert.That(database.GetInteraction(first, second), Is.EqualTo(AnimalInteraction.SecondEatsFirst));

            AnimalTestData.SetRules(database, (first, second, AnimalInteraction.PhysicalContact));

            Assert.That(database.GetInteraction(first, second), Is.EqualTo(AnimalInteraction.PhysicalContact));
        }
    }
}
