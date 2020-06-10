import React, { Component } from 'react';

import { Link } from 'react-router-dom';

export class Channels extends Component {
    static displayName = Channels.name;
    

    constructor(props) {
        super(props);
        var drId = "";
        var showResults = false;
        var str = props.location.pathname;
        var n = str.lastIndexOf('/');
        var result = str.substring(n + 1);
        if (result.toLowerCase() != "LiveChannels") {
            showResults = true;
        }

        this.state = {
            stations: [],
            loading_stations: true,
            loading_results: true,
            selectedChannel: "",
            drId: "",
            url: "",
            type: "",
            name: "",
            results: [],
            start: "",
            end: "",
            showResults: showResults
        };
        this.startRadio = this.startRadio.bind(this);
        this.stopRadio = this.stopRadio.bind(this);
        this.changeSelected = this.changeSelected.bind(this);
        this.postChannel = this.postChannel.bind(this);
        this.changeUrl = this.changeUrl.bind(this);
        this.changeChannelName = this.changeChannelName.bind(this);
        this.changeDrId = this.changeDrId.bind(this);
        this.changeType = this.changeType.bind(this);
    }

    componentDidMount() {
        var iniStart = new Date(Date.now()).toISOString().slice(0, 16);
        var iniEnd = new Date(Date.now() + 3600000).toISOString().slice(0, 16);
        this.FetchStations();

        this.setState({ end: iniEnd, start: iniStart })
    }

    async FetchStations() {
        const response = await fetch('api/channels');
        const data = await response.json();
        this.setState({ stations: data, selectedChannel: data[0].drId,loading: false });
    }

    async startRadio() {
        this.setState({ loading: true });
        const settings = {
            method: "post"
        };
        const url = "api/channels/" + this.state.selectedChannel + "/start";

        const response = await fetch(url, settings);
        const data = await response.json();
        this.setState({ stations: data, loading: false });

    }

    async stopRadio() {
        this.setState({ loading: true });
        const settings = {
            method: "post"
        };
        const url = "api/channels/" + this.state.selectedChannel + "/stop";

        const response = await fetch(url, settings);
        const data = await response.json();

        this.setState({ stations: data, loading: false });
    }

    
    changeSelected(event) {
        console.log(event);
        this.setState({ selectedChannel: event.target.value });
}

changeUrl(event) {
    console.log(event);
    this.setState({ url: event.target.value });
}
changeChannelName(event) {
    console.log(event);
    this.setState({ name: event.target.value });
}
changeDrId(event) {
    console.log(event);
    this.setState({ drId: event.target.value });
}
changeType(event) {
    console.log(event);
    this.setState({ type: event.target.value });
}


    makeSelect() {

        var indents = this.state.stations.map(function (s) {
            return (
                <option value={s.drId}>{s.channelName}</option>
                );
           });
        
        return (
            <select id="selectChannel" onChange={this.changeSelected}>
                {indents}
                </select>
          );
    }

    renderResultsTable(results) {
        return (
            <table className='table table-striped' aria-labelledby="tabelLabel">
                <thead>
                    <tr>
                        <th>reference</th>
                        <th>title</th>
                        <th>artists</th>
                        <th>startTime</th>
                        <th>endTime</th>
                        <th>duration</th>
                        <th>accuracy</th>
                    </tr>
                </thead>
                <tbody>
                    {results.map(r =>
                        <tr key={r.id}>
                            <td>{r.reference}</td>
                            <td>{r.title}</td>
                            <td>{r.artists}</td>
                            <td>{r.start_time}</td>
                            <td>{r.end_time}</td>
                            <td>{r.duration}</td>
                            <td>{r.accuracy}</td>
                        </tr>
                    )}
                </tbody>
            </table>
        );
    }
        
    renderStationTable(stations) {
        return (
            <table className='table table-striped' aria-labelledby="tabelLabel">
                <thead>
                    <tr>
                        <th>channelDrId</th>
                        <th>channelName</th>
                        <th>channelType</th>
                        <th>channelUrl</th>
                        <th>channelActive</th>
                    </tr>
                </thead>
                <tbody>
                    {stations.map(f =>
                        <tr key={f.drId}>
                            <td><Link onClick={this.forceUpdate} to={{ pathname: '/LivestreamResults/' +f.drId, aboutProps: { drId: f.drId } }}>{f.drId}</Link></td>
                            <td>{f.channelName}</td>
                            <td>{f.channelType}</td>
                            <td>{f.streamingUrl}</td>
                            <td>{f.running ? "true" : "false"}</td>

                        </tr>
                    )}
                </tbody>
            </table>
        );
    }

    async postChannel(e) {
        e.preventDefault();
        this.setState({ loading: true });
        const input = { "drId": this.state.drId, "url": this.state.url, "type": this.state.type, "name": this.state.channel_name };
        const settings = {
            method: "post",
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(input)
        };
        const url = "api/channels/";

        const response = await fetch(url, settings);
        const data = await response.text();

        this.setState({ loading: false });
        alert("A channel was submitted: " + data);
        this.FetchTracks();
    }

    async getResults(e) {
        if (e != undefined) e.preventDefault();
        this.setState({ loading: true });
        const settings = {
            method: "get",
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
        };
        const begin = this.state.start != "" ? "&begin=" + this.state.start : "";
        const end = this.state.end != "" ? "&end=" + this.state.end : "";

        const url = "api/channels/" + this.state.selectedChannel + "/results?filters=" + this.state.filters + begin + end;

        const response = await fetch(url, settings);
        const data = await response.json();


        this.setState({ results: data == undefined ? [] : data.results, loading: false });
        //  alert("A file was submitted for analysis: " + data);
    }

    render() {
        if (this.state.showResults) {
            let contents = this.state.loading
                ? <p><em>Loading...</em></p>
                : this.renderResultsTable(this.state.results);

            return (
                <div>
                    <h1 id="tabelLabel" >Channel Results</h1>

                    {this.makeSelect()}

                    <form onSubmit={this.getResults}>
                        <label>
                            Start time:
                    <input type="text" value={this.state.start} placeholder={new Date(Date.now() - 3600000).toISOString().slice(0, 16)} onChange={this.changeStart} />
                        </label>
                        <label>
                            End time:
                    <input type="text" value={this.state.end} placeholder={new Date().toISOString().slice(0, 16)} onChange={this.changeEnd} />
                        </label>
                        <label>
                            filters:
                    <input type="checkbox" name="filters" checked={this.state.filters} onChange={this.changeFilters} />
                        </label>
                        <input type="submit" text="Get results" />
                    </form>


                    <br />
                    {contents}
                </div>
            );
        }
        else {
            let contents = this.state.loading
                ? <p><em>Loading...</em></p>
                : this.renderStationTable(this.state.stations);

            return (
                <div>
                    <h1>Radio</h1>

                    <p>Testing Radio</p>

                    {this.makeSelect()}

                    <button className="btn btn-primary" onClick={this.startRadio}>Start {this.state.selectedChannel}</button>

                    <button className="btn btn-primary" onClick={this.stopRadio}>Stop {this.state.selectedChannel}</button>
                    <br />

                    <form onSubmit={this.postChannel}>
                        <label>
                            Dr Id:
                        <input type="text" name="Dr Id" onChange={this.changeDrId} required />
                        </label>
                        <label>
                            URL:
                        <input type="text" name="URL" onChange={this.changeUrl} required />
                        </label>
                        <label>
                            Channel Type:
                        <input type="text" placeholder="Radio" name="Channel Type" onChange={this.changeType} required />
                        </label>
                        <label>
                            Channel Name:
                        <input type="text" name="Channel Name" onChange={this.changeChannelName} required />
                        </label>
                        <input type="submit" text="Submit channel" />
                    </form>
                    {contents}
                </div>
            );
        }
    }
}
