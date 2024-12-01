from flask import Flask, request, jsonify, Response
from tinytroupe.examples import create_lisa_the_data_scientist, create_oscar_the_architect,>
from tinytroupe.environment import TinyWorld
import json
import queue
import threading

app = Flask(__name__)

# Create a message queue for streaming
message_queue = queue.Queue()

# Initialize agents and environment
lisa = create_lisa_the_data_scientist()
oscar = create_oscar_the_architect()
emma = create_emma_the_hr_manager()
derek = create_derek_the_it_manager()
world = TinyWorld("Chat Room", [lisa, oscar, emma, derek])

def process_simulation(prompt, steps):
    """Process the simulation and put messages in the queue"""
    world.clear_communications_buffer()
    lisa.listen(prompt)
    
    # Override the world's _push_and_display_latest_communication method temporarily
    original_push_display = world._push_and_display_latest_communication
    
    def custom_push_display(rendering):
        # Put the message in the queue and call original method
        if isinstance(rendering, dict):
            message = rendering["content"]
        else:
            message = str(rendering)
        message_queue.put(message)
        original_push_display(rendering)
    
    # Replace the method
    world._push_and_display_latest_communication = custom_push_display
    
    try:
        world.run(steps)
    finally:
        # Restore original method
        world._push_and_display_latest_communication = original_push_display
        # Signal completion
        message_queue.put(None)

@app.route("/stream_conversation", methods=["POST"])
def stream_conversation():
    data = request.json
    prompt = data["prompt"]
    steps = data.get("steps", 4)
    
    # Clear the queue
    while not message_queue.empty():
        message_queue.get()
    
    # Start processing in a separate thread
    thread = threading.Thread(target=process_simulation, args=(prompt, steps))
    thread.start()
    
    def generate():
        while True:
            message = message_queue.get()
            if message is None:  # End of conversation
                break
            yield f"data: {json.dumps({'message': message})}\n\n"
    
    return Response(
        generate(),
        mimetype='text/event-stream',
        headers={
            'Cache-Control': 'no-cache',
            'X-Accel-Buffering': 'no'
        }
    )

@app.after_request
def add_cors_headers(response):
    response.headers['Access-Control-Allow-Origin'] = '*'
    response.headers['Access-Control-Allow-Headers'] = 'Content-Type'
    response.headers['Access-Control-Allow-Methods'] = 'POST'
    return response

if __name__ == "__main__":
    app.run(host="0.0.0.0", port=8000, threaded=True)


