using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bee.Eee.Utility.Logging
{
    /// <summary>
    /// Interface for handling logging categories
    /// </summary>
    public interface ICategoryManager
    {
        void RegisterCategory(int categoryId, string categoryName);
        void RegisterCategories(object categories);
        void EnableCategory(int categoryId);
        void DisableCategroy(int categoryId);
        bool IsCategoryEnabled(int categoryId);
        void SetEnabledCategories(int[] categories);        
    }

    /// <summary>
    /// The implementation of the CategoryMananger
    /// </summary>
    class CategoryManager : ICategoryManager
    {
        private int[] _enabledCategories;
        private Dictionary<int, string> _categories;

        public CategoryManager()
        {
            _categories = new Dictionary<int, string>();
            _enabledCategories = new int[0];
        }

        public void DisableCategroy(int categoryId)
        {
            if (_enabledCategories.Length == 0)
                return;

            var n = new int[_enabledCategories.Length - 1];
            int ii = 0;
            for (int i = 0; i < _enabledCategories.Length; i++)
            {
                if (_enabledCategories[i] != categoryId)
                    n[ii++] = _enabledCategories[i];
            }

            _enabledCategories = n;
        }

        public void EnableCategory(int categoryId)
        {
            var n = new int[_enabledCategories.Length + 1];
            for (int i = 0; i < _enabledCategories.Length; i++)
            {
                // don't add duplicates
                if (_enabledCategories[i] == categoryId)
                    return;

                n[i] = _enabledCategories[i];
            }

            n[_enabledCategories.Length] = categoryId;
        }

        public bool IsCategoryEnabled(int categoryId)
        {
            for (int i = 0; i < _enabledCategories.Length; i++)
                if (_enabledCategories[i] == categoryId)
                    return true;

            return false;
        }

        public void RegisterCategory(int categoryId, string categoryName)
        {
            _categories[categoryId] = categoryName;
        }

        public void RegisterCategories(object categories)
        {
            if (null == categories)
                throw new ArgumentNullException("categories");

            Type t = categories.GetType();
            foreach (var prop in t.GetProperties())
            {
                if (prop.PropertyType == typeof(int))
                {
                    int id = (int)prop.GetValue(categories);
                    _categories.Add(id, prop.Name);
                }
            }
        }

        public void SetEnabledCategories(int[] categories)
        {
            _enabledCategories = categories;
        }
    }
}
