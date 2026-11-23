using Rise.Common.Extensions.Markup;
using Rise.Data.ViewModels;
using Rise.Models;

namespace Rise.App.ViewModels
{
    public sealed class GenreViewModel : ViewModel<Genre>
    {

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the GenreViewModel class that wraps a Genre object.
        /// </summary>
        public GenreViewModel(Genre model = null)
        {
            Model = model ?? new Genre();
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the genre name.
        /// </summary>
        public string Name
        {
            get
            {
                if (Model.Name == "UnknownGenreResource")
                    return ResourceHelper.GetString("UnknownGenreResource");
                return Model.Name;
            }
            set
            {
                if (value != Model.Name)
                {
                    Model.Name = value;
                    OnPropertyChanged();
                }
            }
        }
        #endregion
    }
}
