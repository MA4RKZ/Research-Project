# app.py - Run this on a Python environment with Flask, NumPy, Pandas and SciPy installed
from flask import Flask, request, jsonify
from flask_cors import CORS  # Você precisará instalar: pip install flask-cors
import numpy as np
import pandas as pd
from scipy import signal
import logging

# Configure logging
logging.basicConfig(level=logging.INFO, 
                    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s')
logger = logging.getLogger(__name__)

app = Flask(__name__)
CORS(app)  # Habilita CORS para todas as rotas

@app.route('/')
def home():
    logger.info("Root endpoint accessed")
    return "Breathing Data Processing API - Use /process_data endpoint with POST requests"

@app.route('/process_data', methods=['POST'])
def process_data():
    logger.info("Process data endpoint accessed")
    
    # Log request details
    logger.info(f"Request headers: {request.headers}")
    logger.info(f"Request content type: {request.content_type}")
    
    try:
        # Get data from request
        data = request.json
        logger.info(f"Received data: {data}")
        
        # Check if data is in the expected format
        if not data:
            logger.warning("No data received")
            return jsonify({"error": "No data received"}), 400
        
        # Extract timestamps and rms values
        timestamps = data.get('timestamps', [])
        rms_values = data.get('rms_values', [])
        
        if not timestamps or not rms_values:
            # Try alternative format (Unity's JsonUtility serialization)
            if 'keys' in data and 'values' in data:
                keys = data.get('keys', [])
                values = data.get('values', [])
                
                if 'timestamps' in keys and 'rms_values' in keys:
                    timestamps_index = keys.index('timestamps')
                    rms_values_index = keys.index('rms_values')
                    
                    if timestamps_index < len(values) and rms_values_index < len(values):
                        timestamps = values[timestamps_index]
                        rms_values = values[rms_values_index]
                        logger.info("Extracted data from alternative format")
        
        if not timestamps or not rms_values:
            logger.warning("Could not extract timestamps or rms_values from data")
            return jsonify({"error": "Invalid data format"}), 400
        
        logger.info(f"Timestamps count: {len(timestamps)}, RMS values count: {len(rms_values)}")
        
        # Convert to DataFrame
        df = pd.DataFrame({
            'timestamp': timestamps,
            'rms': rms_values
        })
        
        # Apply filters as requested
        filter_type = request.args.get('filter', 'none')
        window_size = int(request.args.get('window_size', 5))
        
        logger.info(f"Applying filter: {filter_type} with window size: {window_size}")
        
        if filter_type == 'moving_average':
            # Apply moving average
            df['filtered_rms'] = df['rms'].rolling(window=window_size, center=True).mean().fillna(df['rms'])
        elif filter_type == 'lowpass':
            # Apply lowpass filter
            b, a = signal.butter(3, 0.1)
            df['filtered_rms'] = signal.filtfilt(b, a, df['rms'])
        elif filter_type == 'adaptive':
            # Apply adaptive filter
            df['filtered_rms'] = df['rms'].copy()
            alpha = 0.1
            for i in range(1, len(df)):
                change = abs(df.iloc[i]['rms'] - df.iloc[i-1]['rms'])
                adaptive_alpha = min(max(alpha * (1 + change * 5), 0.1), 0.9)
                df.iloc[i, df.columns.get_loc('filtered_rms')] = adaptive_alpha * df.iloc[i]['rms'] + (1 - adaptive_alpha) * df.iloc[i-1]['filtered_rms']
        else:
            # No filter
            df['filtered_rms'] = df['rms']
        
        # Calcular estatísticas (apenas para log, não retornamos mais isso)
        mean_rms = float(df['rms'].mean())
        max_rms = float(df['rms'].max())
        duration = float(df['timestamp'].max() - df['timestamp'].min()) if len(timestamps) > 0 else 0
        
        logger.info(f"Statistics: mean_rms={mean_rms}, max_rms={max_rms}, duration={duration}")
        
        # MUDANÇA IMPORTANTE: Retornar apenas o array de valores filtrados no formato que o GraphVisualizer espera
        result = {
            'filtered_values': df['filtered_rms'].tolist()
        }
        
        logger.info("Processing completed successfully")
        logger.info(f"Returning {len(result['filtered_values'])} filtered values")
        
        return jsonify(result)
    
    except Exception as e:
        logger.error(f"Error processing data: {str(e)}", exc_info=True)
        return jsonify({"error": str(e)}), 500

if __name__ == '__main__':
    logger.info("Starting server on 0.0.0.0:5000")
    app.run(host='0.0.0.0', port=5000)
