namespace Happy_Apps_Core.Database
{
    public class DbTag
    {
        public int Id { get; set; }
        
        public int TagId { get; set; }

        public double Score { get; set; }

        public int Spoiler { get; set; }

        public StaticHelpers.TagCategory? Category { get; set; }

        public virtual ListedVN ListedVN { get; set; }

        private void SetCategory()
        {
            string cat = DumpFiles.PlainTags.Find(item => item.ID == TagId)?.Cat;
            switch (cat)
            {
                case StaticHelpers.ContentTag:
                    Category = StaticHelpers.TagCategory.Content;
                    return;
                case StaticHelpers.SexualTag:
                    Category = StaticHelpers.TagCategory.Sexual;
                    return;
                case StaticHelpers.TechnicalTag:
                    Category = StaticHelpers.TagCategory.Technical;
                    return;
                default:
                    return;
            }
        }
        /// <summary>
        /// Return string with Tag name and score, if tag isn't found in list, "Not Approved" is returned.
        /// </summary>
        /// <returns>String with tag name and score</returns>
        public string Print()
        {
            var name = DumpFiles.PlainTags.Find(item => item.ID == TagId)?.Name;
            return name != null ? $"{name} ({Score:0.00})" : "Not Approved";
        }

        public static DbTag From(VNItem.TagItem tag)
        {
            var result = new DbTag
            {
                Score = tag.Score,
                Spoiler = tag.Spoiler,
                TagId = tag.ID
            };
            result.SetCategory();
            return result;
        }
    }
}