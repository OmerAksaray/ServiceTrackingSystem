// Prevent duplicate declarations
if (typeof window.YandexMapsHelper === 'undefined') {
    console.log("Initializing YandexMapsHelper for the first time");
    
    window.YandexMapsHelper = (() => {
        let apiKey = ""; // Will be set via init function
        let map;

        /* -------- PUBLICS -------- */
        return {
            generateExternalUrl,
            initializeMapFromGeometry,
            startNavigation,
            setApiKey: function(key) { 
                console.log("Setting Yandex API key:", key ? "Provided" : "Missing"); 
                apiKey = key; 
            }
        };

        /* -------- IMPLEMENTATION -------- */
        function generateExternalUrl(addresses) {
            console.log("Generating external URL for addresses:", addresses);
            return `https://yandex.com/maps/?rtext=${encodeURIComponent(addresses.join('~'))}&rtt=auto`;
        }

        async function initializeMapFromGeometry(containerId, coords) {
            console.log("Initializing map with coords:", coords);
            
            if (!coords || !Array.isArray(coords) || coords.length < 2) {
                console.error("Invalid coordinates provided to initializeMapFromGeometry:", coords);
                alert("Harita için geçersiz koordinatlar");
                return;
            }
            
            try {
                await loadApi();
                console.log("Yandex Maps API loaded successfully");
                
                if (!map) {
                    console.log("Creating new map instance");
                    map = new ymaps.Map(containerId, { center: coords[0], zoom: 12 });
                } else {
                    console.log("Using existing map instance");
                }
                
                map.geoObjects.removeAll();
                console.log("Adding multi-route to map");
                
                const multiRoute = new ymaps.multiRouter.MultiRoute({
                    referencePoints: coords,
                    params: { routingMode: 'auto' }
                }, { boundsAutoApply: true });

                map.geoObjects.add(multiRoute);
                console.log("Multi-route added to map");
            } catch (error) {
                console.error("Error initializing map:", error);
                alert("Harita yüklenirken bir hata oluştu: " + error.message);
            }
        }

        function startNavigation(coords) {
            console.log("StartNavigation called with coordinates:", coords);
            
            if (!coords || !Array.isArray(coords) || coords.length < 2) {
                console.error("Invalid coordinates provided to startNavigation:", coords);
                alert("Navigasyon için geçersiz koordinatlar");
                return;
            }
            
            try {
                // Generate URL for both Yandex.Maps browser and Yandex.Navigator app
                const [lat_from, lon_from] = coords[0];
                const [lat_to, lon_to] = coords[coords.length - 1];
                
                // Log coordinates explicitly
                console.log(`From: lat=${lat_from}, lon=${lon_from}`);
                console.log(`To: lat=${lat_to}, lon=${lon_to}`);
                
                // Try deep link for mobile app first
                const mobileUrl = `yandexnavi://build_route_on_map?lat_from=${lat_from}&lon_from=${lon_from}&lat_to=${lat_to}&lon_to=${lon_to}`;
                console.log("Mobile navigation URL:", mobileUrl);
                
                // Fallback URL for browser
                const browserUrl = `https://yandex.com/maps/?rtext=${coords.map(c => `${c[0]},${c[1]}`).join('~')}&rtt=auto`;
                console.log("Browser navigation URL:", browserUrl);
                
                // Try to open the mobile app first
                const isMobile = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
                console.log("Is mobile device:", isMobile);
                
                if (isMobile) {
                    // On mobile, try deep link then web fallback
                    console.log("Attempting to open mobile app");
                    setTimeout(function() {
                        console.log("Opening browser URL as fallback");
                        window.location.href = browserUrl; // Fallback if app not installed
                    }, 2000);
                    window.location.href = mobileUrl;
                } else {
                    // On desktop, just open in browser
                    console.log("Opening in desktop browser");
                    window.open(browserUrl, '_blank');
                }
            } catch (error) {
                console.error("Navigation error:", error);
                alert("Navigasyon başlatılırken bir hata oluştu: " + error.message);
            }
        }

        function loadApi() {
            return new Promise(res => {
                if (window.ymaps) {
                    console.log("Yandex Maps API already loaded");
                    return ymaps.ready(res);
                }
                
                console.log("Loading Yandex Maps API with key:", apiKey ? "Provided" : "Missing");
                const s = document.createElement('script');
                s.src = `https://api-maps.yandex.ru/2.1/?apikey=${apiKey}&lang=tr_TR`;
                s.onload = () => {
                    console.log("Yandex Maps script loaded");
                    ymaps.ready(res);
                };
                s.onerror = (error) => {
                    console.error("Failed to load Yandex Maps API:", error);
                    alert("Harita servisi yüklenemedi");
                };
                document.head.appendChild(s);
            });
        }
    })();
} else {
    console.log("YandexMapsHelper already initialized, skipping duplicate initialization");
}