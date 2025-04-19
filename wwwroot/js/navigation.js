/**
 * Navigation utilities for Google Maps integration
 */

const NavigationUtils = {
    /**
     * Start Google Maps navigation from current position to a destination
     * @param {number} destinationLat - Destination latitude
     * @param {number} destinationLng - Destination longitude
     * @param {function} onError - Optional error callback function
     */
    startNavigation: function(destinationLat, destinationLng, onError) {
        if (navigator.geolocation) {
            navigator.geolocation.getCurrentPosition(
                (position) => {
                    const userLat = position.coords.latitude;
                    const userLng = position.coords.longitude;
                    const url = `https://www.google.com/maps/dir/?api=1&origin=${userLat},${userLng}&destination=${destinationLat},${destinationLng}&travelmode=driving`;
                    window.open(url, '_blank');
                },
                (error) => {
                    console.error("Geolocation error:", error);
                    if (typeof onError === 'function') {
                        onError(error);
                    } else {
                        alert("Konumunuza erişilemiyor. Lütfen konum erişimine izin verin.");
                    }
                }
            );
        } else {
            const errorMsg = "Tarayıcınız konum hizmetlerini desteklemiyor.";
            console.error(errorMsg);
            if (typeof onError === 'function') {
                onError(new Error(errorMsg));
            } else {
                alert(errorMsg);
            }
        }
    },

    /**
     * Start navigation to a location by its ID
     * @param {number} locationId - The ID of the location to navigate to
     * @param {function} onError - Optional error callback function
     */
    navigateToLocationById: function(locationId, onError) {
        fetch(`/Maps/GetLocationCoordinates/${locationId}`)
            .then(response => {
                if (!response.ok) {
                    throw new Error('Server returned ' + response.status);
                }
                return response.json();
            })
            .then(data => {
                this.startNavigation(data.lat, data.lng, onError);
            })
            .catch(error => {
                console.error('Error fetching location data:', error);
                if (typeof onError === 'function') {
                    onError(error);
                } else {
                    alert('Error fetching location data. Please try again.');
                }
            });
    }
};

// Make available globally
window.NavigationUtils = NavigationUtils; 