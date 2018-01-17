namespace Happy_Apps_Core.Database
{
    public class DbTrait
    {
        public int Id { get; set; }

        public int TraitId { get; set; }

        public int Spoiler { get; set; }

        public virtual CharacterItem CharacterItem { get; set; }

        public static DbTrait From(CharacterItem.TraitItem trait)
        {
            var result = new DbTrait
            {
                TraitId = trait.ID,
                Spoiler = trait.Spoiler
            };
            return result;
        }
    }
}